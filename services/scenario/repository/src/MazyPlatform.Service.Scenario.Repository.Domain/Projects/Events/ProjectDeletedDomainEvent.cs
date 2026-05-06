namespace MazyPlatform.Service.Scenario.Repository.Domain.Projects.Events;

public sealed record ProjectDeletedDomainEvent : DomainEventBase
{
    internal ProjectDeletedDomainEvent(DateTimeOffset occurredAt, Guid projectId, Guid ownerAccountId)
        : base(occurredAt)
    {
        ProjectId = projectId;
        OwnerAccountId = ownerAccountId;
    }

    public Guid ProjectId { get; }

    public Guid OwnerAccountId { get; }
}
