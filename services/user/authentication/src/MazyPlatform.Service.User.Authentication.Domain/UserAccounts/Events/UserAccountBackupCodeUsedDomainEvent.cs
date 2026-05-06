namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при успешном использовании резервного кода MFA.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.UseBackupCode"/>.
/// Может использоваться обработчиками для уведомления пользователя об использовании
/// резервного кода, что является признаком потенциальной утраты основного метода MFA.
/// </remarks>
/// <seealso cref="UserAccountDomainEventBase"/>
public sealed record UserAccountBackupCodeUsedDomainEvent : UserAccountDomainEventBase
{
    public UserAccountBackupCodeUsedDomainEvent(DateTimeOffset now, Guid id, Email email)
        : base(now, id, email) { }
}
