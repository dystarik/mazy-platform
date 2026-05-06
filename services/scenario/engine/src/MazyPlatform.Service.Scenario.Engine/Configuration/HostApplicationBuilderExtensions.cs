namespace MazyPlatform.Service.Scenario.Engine.Configuration;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Storage.Mongo.Registration;
using MazyPlatform.Scenario.Telegram.Registration;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.Registration;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Configuration.Options;
using MazyPlatform.Service.Scenario.Engine.Configuration.Options.RabbitMq;
using MazyPlatform.Service.Scenario.Engine.Delays;
using MazyPlatform.Service.Scenario.Engine.Grpc;
using MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;
using MazyPlatform.Service.Scenario.Engine.Messaging;
using MazyPlatform.Service.Scenario.Engine.Messaging.Dispatchers;
using MazyPlatform.Service.Scenario.Engine.Observability;
using MazyPlatform.Service.Scenario.Engine.Scenarios;
using MazyPlatform.Service.Scenario.Engine.Startup;

using Microsoft.Extensions.Options;

using Serilog;

internal static class HostApplicationBuilderExtensions
{
    public static HostApplicationBuilder AddConfigure(this HostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Services
            .AddLogging(configuration)
            .AddDiagnostics()
            .AddMessaging(configuration)
            .AddIntegrationEventHandlers()
            .AddCaches()
            .AddGrpcClients(configuration)
            .AddScenarioLoader()
            .AddInitialSync()
            .AddScenarioEngine(configuration)
            .AddDelayResume(configuration);

        return builder;
    }

    private static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration);
        });

        return services;
    }

    private static IServiceCollection AddDiagnostics(this IServiceCollection services)
    {
        services.AddHostedService<DiagnosticsHostedService>();
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        IntegrationEventDispatcher.BuildCache();

        services.AddSingleton<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddHostedService<RabbitMqConsumerService>();

        return services;
    }

    private static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services)
    {
        return services.Scan(scan => scan
            .FromAssemblyOf<BotIncomingEventIntegrationEventHandler>()
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static IServiceCollection AddCaches(this IServiceCollection services)
    {
        services.AddSingleton<BotInstanceCache>();
        services.AddSingleton<ScenarioCache>();
        return services;
    }

    private static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BotManagerOptions>()
            .Bind(configuration.GetSection(BotManagerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ScenarioRepositoryOptions>()
            .Bind(configuration.GetSection(ScenarioRepositoryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddGrpcClient<BotInternalService.BotInternalServiceClient>((sp, o) =>
        {
            var opts = sp.GetRequiredService<IOptions<BotManagerOptions>>().Value;
            o.Address = new Uri(opts.Address);
        });

        services.AddGrpcClient<ScenarioGraphService.ScenarioGraphServiceClient>((sp, o) =>
        {
            var opts = sp.GetRequiredService<IOptions<ScenarioRepositoryOptions>>().Value;
            o.Address = new Uri(opts.Address);
        });

        services.AddScoped<IBotManagerClient, BotManagerClient>();
        services.AddScoped<IScenarioRepositoryClient, ScenarioRepositoryClient>();

        return services;
    }

    private static IServiceCollection AddScenarioLoader(this IServiceCollection services)
    {
        services.AddScoped<ScenarioLoader>();
        return services;
    }

    private static IServiceCollection AddInitialSync(this IServiceCollection services)
    {
        services.AddHostedService<BotsInitialSyncService>();
        return services;
    }

    private static IServiceCollection AddScenarioEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScenario();

        services.AddVkScenario(options =>
        {
            configuration.GetSection("Vk").Bind(options);
        });

        services.AddTelegramScenario(options =>
        {
            configuration.GetSection("Telegram").Bind(options);
        });

        services.AddMongoStorage(options =>
        {
            options.ConnectionString = configuration["MongoDb:ConnectionString"]
                ?? throw new InvalidOperationException("MongoDb:ConnectionString не задан.");
            options.DatabaseName = configuration["MongoDb:DatabaseName"]
                ?? throw new InvalidOperationException("MongoDb:DatabaseName не задан.");
        });

        return services;
    }

    private static IServiceCollection AddDelayResume(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DelayResumeOptions>()
            .Bind(configuration.GetSection(DelayResumeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<DelaySessionClaimStore>();
        services.AddHostedService<DelayResumeService>();

        return services;
    }
}
