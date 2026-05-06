namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record ValidateScenarioDraftQuery(string ProjectId, string OwnerAccountId) : IQuery<ValidateScenarioDraftResult>;
