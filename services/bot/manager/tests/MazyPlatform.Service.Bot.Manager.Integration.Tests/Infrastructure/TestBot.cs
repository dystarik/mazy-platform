namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using MazyPlatform.Contracts.Bot.Grpc.Manager;

public sealed record TestBot(
    Guid BotInstanceId,
    Guid OwnerAccountId,
    Guid? ProjectId,
    string Name,
    BotPlatformType PlatformType,
    string AccessToken,
    string? CommunityId,
    int? ScenarioVersion);
