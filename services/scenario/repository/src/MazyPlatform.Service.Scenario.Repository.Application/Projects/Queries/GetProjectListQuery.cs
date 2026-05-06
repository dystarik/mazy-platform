namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

public sealed record GetProjectListQuery(string OwnerAccountId) : IQuery<GetProjectListResult>;
