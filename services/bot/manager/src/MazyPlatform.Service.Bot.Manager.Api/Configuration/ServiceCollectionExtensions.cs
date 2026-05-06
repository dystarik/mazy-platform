namespace MazyPlatform.Service.Bot.Manager.Api.Configuration;

using MazyPlatform.Service.Bot.Manager.Api.Common.Interceptors;
using MazyPlatform.Service.Bot.Manager.Api.Services;
using MazyPlatform.Service.Bot.Manager.Application;
using MazyPlatform.Service.Bot.Manager.Domain;
using MazyPlatform.Service.Bot.Manager.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc(o =>
            {
                o.Interceptors.Add<TraceIdInterceptor>();
                o.Interceptors.Add<LoggingInterceptor>();
            })
            .AddServiceOptions<BotInternalGrpcService>(o => o.Interceptors.Add<InternalApiAuthInterceptor>());

        services.AddTransient<InternalApiAuthInterceptor>();
        services.AddTransient<TraceIdInterceptor>();
        services.AddTransient<LoggingInterceptor>();

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Менеджер ботов запущен."), tags: ["live"])
            .AddCheck<BotManagerConfigurationHealthCheck>("configuration", tags: ["ready"]);

        services.AddOptions<InternalApiOptions>()
            .BindConfiguration(InternalApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddDomainLayer();
        services.AddApplicationLayer();
        services.AddInfrastructureLayer(configuration);

        return services;
    }
}
