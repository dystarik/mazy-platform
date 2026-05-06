namespace MazyPlatform.Service.User.Authentication.Domain;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainLayer(this IServiceCollection services)
    {
        return services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(c => c.AssignableTo<IDomainService>(), publicOnly: false)
            .AsSelf()
            .WithScopedLifetime());
    }
}
