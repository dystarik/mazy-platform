namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas.ValueObjects;

public sealed record GetEntitySchemaResult(Guid SchemaId, string Name, IReadOnlyList<GetEntitySchemaResult.EntityField> Fields)
{
    public sealed record EntityField(Guid FieldId, string Name, FieldType FieldType, bool IsRequired, string? DefaultValue);
}
