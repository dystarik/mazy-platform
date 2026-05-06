namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record GetVersionHistoryQuery(string ProjectId, string OwnerAccountId) : IQuery<GetVersionHistoryResult>;
