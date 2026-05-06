namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record GetReleasedScenarioResult(Guid ScenarioGraphId, string GraphJson, int Version);
