namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при добавлении нового метода MFA к аккаунту.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.AddMfaMethod"/>.
/// Метод может находиться в состоянии ожидания подтверждения; подтверждение
/// фиксируется отдельным событием <see cref="UserAccountMfaMethodConfirmedDomainEvent"/>.
/// </remarks>
/// <seealso cref="UserAccountMfaMethodDomainEventBase"/>
public sealed record UserAccountMfaMethodAddedDomainEvent : UserAccountMfaMethodDomainEventBase
{
    public UserAccountMfaMethodAddedDomainEvent(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        MfaMethodType methodType)
    : base(occurredAt, userId, email, methodType) { }
}
