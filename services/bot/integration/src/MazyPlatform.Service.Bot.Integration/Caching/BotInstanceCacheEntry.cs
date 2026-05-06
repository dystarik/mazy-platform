namespace MazyPlatform.Service.Bot.Integration.Caching;

using MazyPlatform.Contracts.Bot;

internal sealed record BotInstanceCacheEntry(
    Guid BotInstanceId,
    Guid ProjectId,
    int ScenarioVersion,
    PlatformType PlatformType,
    int? VkCommunityId,
    string AccessToken)
{
    public int RequiredVkCommunityId => VkCommunityId
        ?? throw new InvalidOperationException($"VK community id is not set for bot '{BotInstanceId}'.");
}
