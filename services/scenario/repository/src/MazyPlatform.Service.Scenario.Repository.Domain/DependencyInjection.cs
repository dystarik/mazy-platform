namespace MazyPlatform.Service.Scenario.Repository.Domain;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainLayer(this IServiceCollection services)
    {
        services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(c => c.AssignableTo<IScenarioSpecification>(), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }
}
