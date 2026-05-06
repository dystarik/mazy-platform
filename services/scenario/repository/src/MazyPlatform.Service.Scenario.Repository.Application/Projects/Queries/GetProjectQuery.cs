namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

public sealed record GetProjectQuery(string ProjectId, string OwnerAccountId) : IQuery<GetProjectResult>;
