namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

public sealed record GetEntitySchemaListQuery(string ProjectId, string OwnerAccountId) : IQuery<GetEntitySchemaListResult>;
