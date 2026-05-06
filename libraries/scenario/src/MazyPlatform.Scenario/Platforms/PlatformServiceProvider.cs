namespace MazyPlatform.Scenario.Platforms;

using Microsoft.Extensions.DependencyInjection;

internal sealed class PlatformServiceProvider(IServiceProvider rootProvider, IReadOnlyDictionary<Type, Type> serviceTypeMap) : IServiceProvider
{
    public object? GetService(Type serviceType)
    {
        if (serviceTypeMap.TryGetValue(serviceType, out var implementationType))
            return rootProvider.GetRequiredService(implementationType);

        return rootProvider.GetService(serviceType);
    }
}
