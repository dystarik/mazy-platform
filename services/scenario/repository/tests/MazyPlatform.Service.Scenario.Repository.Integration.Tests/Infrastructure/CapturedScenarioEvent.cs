namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed record CapturedScenarioEvent(
    string RoutingKey,
    DateTimeOffset CapturedAt,
    Guid? ProjectId,
    int? CurrentVersion,
    int? DeletedVersion,
    string Json);
