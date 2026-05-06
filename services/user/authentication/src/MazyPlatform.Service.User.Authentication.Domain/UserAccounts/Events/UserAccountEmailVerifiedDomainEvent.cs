namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при первом подтверждении email аккаунта.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.VerifyEmail"/> через <see cref="CompleteRegistrationService"/>.
/// Поднимается единственный раз в жизненном цикле аккаунта; повторные вызовы <c>VerifyEmail</c> игнорируются.
/// </remarks>
/// <seealso cref="UserAccountDomainEventBase"/>
public sealed record class UserAccountEmailVerifiedDomainEvent : UserAccountDomainEventBase
{
    internal UserAccountEmailVerifiedDomainEvent(DateTimeOffset occurredAt, Guid userId, Email email)
        : base(occurredAt, userId, email) { }
}
