namespace MazyPlatform.Service.Bot.Integration.Grpc;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Bot.Integration.Caching;

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
