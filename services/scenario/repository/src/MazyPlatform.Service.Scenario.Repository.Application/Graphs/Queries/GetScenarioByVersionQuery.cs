namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record GetScenarioByVersionQuery(string ProjectId, int Version) : IQuery<GetScenarioByVersionResult>;
