namespace MazyPlatform.Service.Bot.Manager.Domain.UserAccounts.Events;

/// <summary>
/// Доменное событие, возникающее при создании локальной проекции аккаунта пользователя.
/// </summary>
public sealed record UserAccountCreatedDomainEvent : DomainEventBase
{
    internal UserAccountCreatedDomainEvent(DateTimeOffset occurredAt, Guid userAccountId)
        : base(occurredAt) => UserAccountId = userAccountId;

    /// <summary>Идентификатор аккаунта пользователя.</summary>
    public Guid UserAccountId { get; }
}
