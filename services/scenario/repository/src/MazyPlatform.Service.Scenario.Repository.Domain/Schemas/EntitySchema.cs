namespace MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas.Events;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas.ValueObjects;

public sealed class EntitySchema : AggregateRoot
{
    private readonly List<EntityField> _fields = [];

    private EntitySchema() { }

    public required Guid ProjectId { get; init; }

    public string Name { get; private set; } = null!;

    public IReadOnlyCollection<EntityField> Fields => _fields.AsReadOnly();

    public static EntitySchema Create(Guid projectId, string name, DateTimeOffset now)
    {
        var schema = new EntitySchema
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            CreatedAt = now,
        };
        schema.AddDomainEvent(new EntitySchemaCreatedDomainEvent(now, schema.Id, projectId));
        return schema;
    }

    public void AddField(string name, FieldType fieldType, bool isRequired, string? defaultValue, DateTimeOffset now)
    {
        _fields.Add(EntityField.Create(Id, name, fieldType, isRequired, defaultValue, now));
        MarkAsUpdated(now);
    }

    public void Delete(DateTimeOffset now)
    {
        MarkAsUpdated(now);
        AddDomainEvent(new EntitySchemaDeletedDomainEvent(now, Id, ProjectId));
    }
}
