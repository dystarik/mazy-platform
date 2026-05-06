namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

public sealed record DeleteEntitySchemaCommand(string SchemaId, string OwnerAccountId) : ICommand;
