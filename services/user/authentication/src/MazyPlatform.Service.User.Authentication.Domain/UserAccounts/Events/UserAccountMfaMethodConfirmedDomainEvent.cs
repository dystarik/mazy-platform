namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при подтверждении метода MFA пользователем.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.ConfirmMfaMethod"/>.
/// После подтверждения первого метода дополнительно генерируются резервные коды;
/// их наличие не отражается в этом событии — оно фиксирует только факт подтверждения.
/// </remarks>
/// <seealso cref="UserAccountMfaMethodDomainEventBase"/>
public sealed record UserAccountMfaMethodConfirmedDomainEvent : UserAccountMfaMethodDomainEventBase
{
    public UserAccountMfaMethodConfirmedDomainEvent(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        MfaMethodType methodType)
    : base(occurredAt, userId, email, methodType) { }
}
