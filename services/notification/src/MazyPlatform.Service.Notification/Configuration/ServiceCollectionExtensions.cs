namespace MazyPlatform.Service.Notification.Configuration;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Notification.Configuration.Options;
using MazyPlatform.Service.Notification.Configuration.Options.RabbitMq;
using MazyPlatform.Service.Notification.IntegrationEventHandlers;
using MazyPlatform.Service.Notification.Messaging;
using MazyPlatform.Service.Notification.Messaging.Abstractions;
using MazyPlatform.Service.Notification.Messaging.Dispatchers;

using Serilog;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddLogging(configuration)
            .AddMessaging(configuration)
            .AddIntegrationEventHandler()
            .AddEmail(configuration);
    }

    private static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services);
        });

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

    private static IServiceCollection AddIntegrationEventHandler(this IServiceCollection services)
    {
        return services
            .Scan(scan => scan
                .FromAssemblyOf<UserAccountRegisteredIntegrationEventHandler>()
                .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
    }

    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
