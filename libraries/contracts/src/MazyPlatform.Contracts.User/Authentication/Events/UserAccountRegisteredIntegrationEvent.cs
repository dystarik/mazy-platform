namespace MazyPlatform.Contracts.User.Authentication.Events;

/// <summary>
/// Интеграционное событие регистрации аккаунта пользователя.
/// Публикуется сервисом аутентификации после успешного создания аккаунта.
/// </summary>
[IntegrationEventType("user.authentication.registered")]
public sealed record UserAccountRegisteredIntegrationEvent : UserAccountEmailIntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserAccountRegisteredIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="userAccountId">Уникальный идентификатор аккаунта пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    /// <param name="confirmationCode">Код подтверждения электронной почты.</param>
    public UserAccountRegisteredIntegrationEvent(
        DateTimeOffset occurredAt,
        Guid userAccountId,
        string email,
        string confirmationCode)
        : base(occurredAt, userAccountId, email) => ConfirmationCode = confirmationCode;

    /// <summary>
    /// Шестизначный код подтверждения электронной почты.
    /// </summary>
    public string ConfirmationCode { get; }
}
