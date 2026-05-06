namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при первой установке локального пароля аккаунта.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.SetPassword"/>.
/// </remarks>
/// <seealso cref="UserAccountDomainEventBase"/>
public sealed record UserAccountPasswordSetDomainEvent : UserAccountDomainEventBase
{
    internal UserAccountPasswordSetDomainEvent(DateTimeOffset occurredAt, Guid userAccountId, Email email)
        : base(occurredAt, userAccountId, email) { }
}
