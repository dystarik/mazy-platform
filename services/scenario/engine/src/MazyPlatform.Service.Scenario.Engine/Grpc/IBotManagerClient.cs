namespace MazyPlatform.Service.Scenario.Engine.Grpc;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Scenario.Engine.Caching;

internal interface IBotManagerClient
{
    IAsyncEnumerable<BotInstanceCacheEntry> StreamActiveBotsAsync(CancellationToken cancellationToken = default);

    Task<BotInstanceCacheEntry?> GetBotCacheEntryAsync(
        Guid botInstanceId,
        Guid projectId,
        int scenarioVersion,
        PlatformType platformType,
        CancellationToken cancellationToken = default);
}
