namespace MazyPlatform.Service.Scenario.Repository.Infrastructure;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;
using MazyPlatform.Service.Scenario.Repository.Infrastructure.Database;
using MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging;
using MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging.Grpc;
using MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging.RabbitMq;
using MazyPlatform.Service.Scenario.Repository.Infrastructure.RuntimeData;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSharedKernelInfrastructure()
            .AddDatabase(configuration)
            .AddScenarioSpecifications()
            .AddGrpcClients(configuration)
            .AddRuntimeData()
            .AddMessaging(configuration);
    }

    private static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BotManagerClientOptions>()
            .BindConfiguration(BotManagerClientOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddGrpcClient<BotInternalService.BotInternalServiceClient>((sp, o) =>
        {
            var opts = sp.GetRequiredService<IOptions<BotManagerClientOptions>>().Value;
            o.Address = new Uri(opts.Address);
        });

        services.AddScoped<IBotManagerClient, BotManagerClient>();
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Строка подключения «DefaultConnection» не найдена.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(c => c.AssignableTo(typeof(IRepository<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IReadOnlyApplicationDbContext, ReadOnlyApplicationDbContext>();
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        IntegrationEventDispatcher.BuildCache(typeof(Application.DependencyInjection).Assembly);

        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        services.AddHostedService<RabbitMqStartupService>();
        services.AddHostedService<RabbitMqConsumerService>();

        return services;
    }

    private static IServiceCollection AddRuntimeData(this IServiceCollection services)
    {
        services.AddOptions<RuntimeMongoOptions>()
            .BindConfiguration(RuntimeMongoOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IRuntimeSchemaSnapshotStore, MongoRuntimeSchemaSnapshotStore>();
        services.AddSingleton<IRuntimeUserDataReader, MongoRuntimeUserDataReader>();

        return services;
    }

    private static IServiceCollection AddScenarioSpecifications(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblies(typeof(IScenarioSpecification).Assembly)
            .AddClasses(c => c.AssignableTo<IScenarioSpecification>(), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
