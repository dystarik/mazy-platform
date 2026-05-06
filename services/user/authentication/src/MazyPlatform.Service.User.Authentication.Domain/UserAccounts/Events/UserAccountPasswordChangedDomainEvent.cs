namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменное событие, поднимаемое при каждой смене пароля аккаунта.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="UserAccount.ChangePassword"/>.
/// Обработчики используют событие для аннулирования активных сессий пользователя.
/// </remarks>
/// <seealso cref="UserAccountDomainEventBase"/>
public sealed record UserAccountPasswordChangedDomainEvent : UserAccountDomainEventBase
{
    internal UserAccountPasswordChangedDomainEvent(DateTimeOffset occurredAt, Guid userAccountId, Email email)
        : base(occurredAt, userAccountId, email) { }
}
