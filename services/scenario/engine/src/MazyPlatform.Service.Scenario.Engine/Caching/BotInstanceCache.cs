namespace MazyPlatform.Service.Scenario.Engine.Caching;

using System.Collections.Concurrent;

internal sealed class BotInstanceCache
{
    private readonly ConcurrentDictionary<Guid, BotInstanceCacheEntry> _cache = new();

    public bool TryGet(Guid botInstanceId, out BotInstanceCacheEntry? entry)
    {
        return _cache.TryGetValue(botInstanceId, out entry);
    }

    public void Set(Guid botInstanceId, BotInstanceCacheEntry entry)
    {
        _cache[botInstanceId] = entry;
    }

    public void Remove(Guid botInstanceId)
    {
        _cache.TryRemove(botInstanceId, out _);
    }
}
