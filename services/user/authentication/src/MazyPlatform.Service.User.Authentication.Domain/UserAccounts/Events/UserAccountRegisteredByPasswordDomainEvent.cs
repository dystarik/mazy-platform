namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при регистрации нового аккаунта по паролю.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.RegisterByPassword"/>.
/// Обработчики могут использовать событие для отправки приветственного письма
/// или инициализации связанных агрегатов в других сервисах.
/// </remarks>
/// <seealso cref="UserAccountDomainEventBase"/>
public sealed record UserAccountRegisteredByPasswordDomainEvent : UserAccountDomainEventBase
{
    internal UserAccountRegisteredByPasswordDomainEvent(DateTimeOffset occurredAt, Guid userAccountId, Email email)
        : base(occurredAt, userAccountId, email) { }
}
