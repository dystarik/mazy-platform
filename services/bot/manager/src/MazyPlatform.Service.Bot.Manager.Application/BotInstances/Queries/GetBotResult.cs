namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

public sealed record GetBotResult(
    Guid BotInstanceId,
    Guid? ProjectId,
    string Name,
    PlatformType PlatformType,
    string MaskedAccessToken,
    string? CommunityId,
    int? ScenarioVersion,
    BotStatus Status,
    ScenarioVersionUpdateMode ScenarioVersionUpdateMode);
