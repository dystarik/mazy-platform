namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

public sealed record CapturedBotEvent(
    string RoutingKey,
    DateTimeOffset CapturedAt,
    Guid? BotInstanceId,
    Guid? ProjectId,
    Guid? FormerProjectId,
    Guid? OwnerAccountId,
    int? NewScenarioVersion,
    string Json);
