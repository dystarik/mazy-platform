namespace MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas.ValueObjects;

public sealed class EntityField : Entity
{
    private EntityField() { }

    public required Guid SchemaId { get; init; }

    public string Name { get; private set; } = null!;

    public FieldType FieldType { get; private set; }

    public bool IsRequired { get; private set; }

    public string? DefaultValue { get; private set; }

    public static EntityField Create(Guid schemaId, string name, FieldType fieldType, bool isRequired, string? defaultValue, DateTimeOffset now)
    {
        return new EntityField
        {
            Id = Guid.NewGuid(),
            SchemaId = schemaId,
            Name = name,
            FieldType = fieldType,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            CreatedAt = now,
        };
    }
}
