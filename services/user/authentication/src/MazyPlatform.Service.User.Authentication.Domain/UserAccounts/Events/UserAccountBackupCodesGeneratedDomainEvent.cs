namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при генерации нового набора резервных кодов MFA.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.GenerateNewBackupCodes"/>.
/// Поднимается как при первичной генерации (после подтверждения первого метода MFA),
/// так и при повторной через <see cref="RegenerateBackupCodesService"/>.
/// Открытые коды не передаются в событии — они возвращаются непосредственно вызывающему сервису.
/// </remarks>
/// <seealso cref="UserAccountDomainEventBase"/>
public sealed record UserAccountBackupCodesGeneratedDomainEvent : UserAccountDomainEventBase
{
    public UserAccountBackupCodesGeneratedDomainEvent(DateTimeOffset now, Guid id, Email email)
        : base(now, id, email) { }
}
