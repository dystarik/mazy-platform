namespace MazyPlatform.Service.Bot.Integration.Configuration;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Configuration.Options;
using MazyPlatform.Service.Bot.Integration.Grpc;
using MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;
using MazyPlatform.Service.Bot.Integration.LongPoll;
using MazyPlatform.Service.Bot.Integration.Messaging;
using MazyPlatform.Service.Bot.Integration.Messaging.Dispatchers;
using MazyPlatform.Service.Bot.Integration.Observability;
using MazyPlatform.Service.Bot.Integration.Startup;

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
            .AddHttpClients()
            .AddCaches()
            .AddGrpcClients(configuration)
            .AddMessaging(configuration)
            .AddIntegrationEventHandlers()
            .AddLongPoll(configuration)
            .AddInitialSync();

        return builder;
    }

    private static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((_, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(configuration));
        return services;
    }

    private static IServiceCollection AddDiagnostics(this IServiceCollection services)
    {
        services.AddHostedService<DiagnosticsHostedService>();
        return services;
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("VkApi", client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient("VkLongPoll", client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient("TelegramApi", client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        return services;
    }

    private static IServiceCollection AddCaches(this IServiceCollection services)
    {
        services.AddSingleton<BotInstanceCache>();
        return services;
    }

    private static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BotManagerOptions>()
            .Bind(configuration.GetSection(BotManagerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddGrpcClient<BotInternalService.BotInternalServiceClient>((sp, o) =>
        {
            var opts = sp.GetRequiredService<IOptions<BotManagerOptions>>().Value;
            o.Address = new Uri(opts.Address);
        });

        services.AddScoped<IBotManagerClient, BotManagerClient>();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        IntegrationEventDispatcher.BuildCache();

        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddHostedService<RabbitMqConsumerService>();

        return services;
    }

    private static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services)
    {
        return services.Scan(scan => scan
            .FromAssemblyOf<BotInstanceActivatedIntegrationEventHandler>()
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static IServiceCollection AddLongPoll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VkOptions>()
            .Bind(configuration.GetSection(VkOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<VkLongPollClient>();
        services.AddSingleton<TelegramPollingClient>();
        services.AddSingleton<BotPollerOrchestrator>();

        return services;
    }

    private static IServiceCollection AddInitialSync(this IServiceCollection services)
    {
        services.AddHostedService<BotsInitialSyncService>();
        return services;
    }
}
