namespace MazyPlatform.Contracts.User.Authentication.Events;

/// <summary>
/// Интеграционное событие запроса сброса пароля аккаунта пользователя.
/// Публикуется сервисом аутентификации после успешного создания OTP для сброса пароля.
/// </summary>
[IntegrationEventType("user.authentication.password-reset-requested")]
public sealed record UserAccountPasswordResetRequestedIntegrationEvent : UserAccountEmailIntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserAccountPasswordResetRequestedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="userAccountId">Уникальный идентификатор аккаунта пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    /// <param name="resetCode">Шестизначный код для сброса пароля.</param>
    public UserAccountPasswordResetRequestedIntegrationEvent(
        DateTimeOffset occurredAt,
        Guid userAccountId,
        string email,
        string resetCode)
        : base(occurredAt, userAccountId, email) => ResetCode = resetCode;

    /// <summary>
    /// Шестизначный код для сброса пароля отправленный на электронную почту.
    /// </summary>
    public string ResetCode { get; }
}
