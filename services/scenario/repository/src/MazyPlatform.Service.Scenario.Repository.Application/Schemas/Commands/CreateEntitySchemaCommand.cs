namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

public sealed record CreateEntitySchemaCommand(string ProjectId, string OwnerAccountId, string Name) : ICommand<CreateEntitySchemaResult>;
