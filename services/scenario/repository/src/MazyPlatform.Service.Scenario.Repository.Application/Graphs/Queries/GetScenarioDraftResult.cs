namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record GetScenarioDraftResult(Guid ScenarioGraphId, string GraphJson, int Version);
