namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record GetScenarioByVersionResult(Guid ScenarioGraphId, string GraphJson, int Version);
