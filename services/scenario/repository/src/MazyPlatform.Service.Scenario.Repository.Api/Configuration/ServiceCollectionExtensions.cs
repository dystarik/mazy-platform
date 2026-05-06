namespace MazyPlatform.Service.Scenario.Repository.Api.Configuration;

using MazyPlatform.Service.Scenario.Repository.Api.Common.Interceptors;
using MazyPlatform.Service.Scenario.Repository.Api.Interceptors;
using MazyPlatform.Service.Scenario.Repository.Api.Services;
using MazyPlatform.Service.Scenario.Repository.Application;
using MazyPlatform.Service.Scenario.Repository.Infrastructure;

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
            .AddServiceOptions<ScenarioRepositoryInternalGrpcService>(o => o.Interceptors.Add<InternalApiAuthInterceptor>());

        services.AddTransient<InternalApiAuthInterceptor>();

        services.AddOptions<InternalApiOptions>()
            .BindConfiguration(InternalApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<ScenarioRepositoryConfigurationHealthCheck>("configuration", tags: ["ready"]);
        services.AddApplicationLayer();
        services.AddInfrastructureLayer(configuration);

        return services;
    }
}
