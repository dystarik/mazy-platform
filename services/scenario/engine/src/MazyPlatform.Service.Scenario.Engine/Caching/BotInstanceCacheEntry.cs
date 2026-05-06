namespace MazyPlatform.Service.Scenario.Engine.Caching;

using MazyPlatform.Contracts.Bot;

internal sealed record BotInstanceCacheEntry(
    Guid BotInstanceId,
    Guid ProjectId,
    int ScenarioVersion,
    PlatformType PlatformType,
    string PlatformKey,
    int? VkCommunityId,
    string AccessToken);
