namespace MazyPlatform.Service.Scenario.Engine.Caching;

using System.Collections.Concurrent;

using MazyPlatform.Scenario.Abstractions.Graph;

internal sealed class ScenarioCache
{
    private readonly ConcurrentDictionary<(Guid ProjectId, int Version, string PlatformKey), ScenarioGraph> _cache = new();

    public bool TryGet(Guid projectId, int version, string platformKey, out ScenarioGraph? graph)
    {
        return _cache.TryGetValue((projectId, version, NormalizePlatformKey(platformKey)), out graph);
    }

    public void Set(Guid projectId, int version, string platformKey, ScenarioGraph graph)
    {
        _cache[(projectId, version, NormalizePlatformKey(platformKey))] = graph;
    }

    private static string NormalizePlatformKey(string platformKey) => platformKey.Trim().ToLowerInvariant();
}
