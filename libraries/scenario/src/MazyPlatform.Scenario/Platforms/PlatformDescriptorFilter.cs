namespace MazyPlatform.Scenario.Platforms;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;

internal static class PlatformDescriptorFilter
{
    public static IReadOnlyList<TProvider> Filter<TProvider>(
        IEnumerable<TProvider> providers,
        string platformKey)
        where TProvider : INodeSchemaProvider
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformKey);

        var normalizedPlatformKey = NormalizePlatformKey(platformKey);

        return [.. providers.Where(provider => IsAvailableFor(provider, normalizedPlatformKey))];
    }

    private static bool IsAvailableFor(INodeSchemaProvider provider, string platformKey)
    {
        if (provider is not IPlatformScopedNodeDescriptor scoped)
            return true;

        if (string.Equals(platformKey, ScenarioPlatformKeys.Universal, StringComparison.Ordinal))
            return false;

        return scoped.PlatformKeys.Contains(platformKey);
    }

    private static string NormalizePlatformKey(string platformKey) =>
        platformKey.Trim().ToLowerInvariant();
}
