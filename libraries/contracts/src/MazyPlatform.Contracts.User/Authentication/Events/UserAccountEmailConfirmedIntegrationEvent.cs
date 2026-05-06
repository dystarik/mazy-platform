namespace MazyPlatform.Contracts.User.Authentication.Events;

/// <summary>
/// Интеграционное событие подтверждения электронной почты аккаунта пользователя.
/// Публикуется сервисом аутентификации после успешного подтверждения email и завершения регистрации.
/// </summary>
[IntegrationEventType("user.authentication.email-confirmed")]
public sealed record UserAccountEmailConfirmedIntegrationEvent : UserAccountEmailIntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserAccountEmailConfirmedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="userAccountId">Уникальный идентификатор аккаунта пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    public UserAccountEmailConfirmedIntegrationEvent(
        DateTimeOffset occurredAt,
        Guid userAccountId,
        string email)
        : base(occurredAt, userAccountId, email) { }
}
