namespace MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts.Events;

public sealed record UserAccountCreatedDomainEvent : DomainEventBase
{
    internal UserAccountCreatedDomainEvent(DateTimeOffset occurredAt, Guid userAccountId)
        : base(occurredAt) => UserAccountId = userAccountId;

    public Guid UserAccountId { get; }
}
