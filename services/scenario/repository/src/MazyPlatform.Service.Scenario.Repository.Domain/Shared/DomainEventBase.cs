namespace MazyPlatform.Service.Scenario.Repository.Domain.Shared;

using MazyPlatform.SharedKernel.Domain.Abstractions;

public abstract record DomainEventBase(DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
