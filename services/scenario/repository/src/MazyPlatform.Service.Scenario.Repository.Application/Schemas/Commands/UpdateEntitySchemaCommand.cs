namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas.ValueObjects;

public sealed record UpdateEntitySchemaCommand(
    string SchemaId,
    string OwnerAccountId,
    string Name,
    FieldType FieldType,
    bool IsRequired,
    string? DefaultValue) : ICommand;
