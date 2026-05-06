namespace MazyPlatform.Service.Bot.Manager.Infrastructure;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Database;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.Grpc;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.RabbitMq;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Security;
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
            .AddSecurity(configuration)
            .AddMessaging(configuration)
            .AddScenarioRepositoryClient(configuration);
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

    private static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BotTokenEncryptionOptions>()
            .BindConfiguration(BotTokenEncryptionOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IBotTokenEncryptor, AesGcmService>();

        return services;
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

    private static IServiceCollection AddScenarioRepositoryClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ScenarioRepositoryClientOptions>()
            .BindConfiguration(ScenarioRepositoryClientOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddGrpcClient<ScenarioRepositoryInternalService.ScenarioRepositoryInternalServiceClient>((sp, o) =>
        {
            var opts = sp.GetRequiredService<IOptions<ScenarioRepositoryClientOptions>>().Value;
            o.Address = new Uri(opts.Address);
        });

        services.AddScoped<IScenarioRepositoryClient, ScenarioRepositoryClient>();

        return services;
    }
}
