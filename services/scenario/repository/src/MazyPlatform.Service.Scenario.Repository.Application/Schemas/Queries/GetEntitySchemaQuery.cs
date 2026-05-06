namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

public sealed record GetEntitySchemaQuery(string SchemaId, string OwnerAccountId) : IQuery<GetEntitySchemaResult>;
