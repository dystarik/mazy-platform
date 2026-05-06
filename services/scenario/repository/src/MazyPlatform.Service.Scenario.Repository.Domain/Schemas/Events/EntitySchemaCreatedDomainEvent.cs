namespace MazyPlatform.Service.Scenario.Repository.Domain.Schemas.Events;

public sealed record EntitySchemaCreatedDomainEvent : DomainEventBase
{
    internal EntitySchemaCreatedDomainEvent(DateTimeOffset occurredAt, Guid schemaId, Guid projectId)
        : base(occurredAt)
    {
        SchemaId = schemaId;
        ProjectId = projectId;
    }

    public Guid SchemaId { get; }

    public Guid ProjectId { get; }
}
