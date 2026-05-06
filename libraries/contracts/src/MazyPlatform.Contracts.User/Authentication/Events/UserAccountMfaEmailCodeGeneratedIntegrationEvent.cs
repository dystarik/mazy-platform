namespace MazyPlatform.Contracts.User.Authentication.Events;

/// <summary>
/// Интеграционное событие генерации MFA-кода отправленного на электронную почту.
/// Публикуется сервисом аутентификации после успешной генерации кода.
/// </summary>
[IntegrationEventType("user.authentication.mfa-email-code-generated")]
public sealed record UserAccountMfaEmailCodeGeneratedIntegrationEvent : UserAccountEmailIntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserAccountMfaEmailCodeGeneratedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="userAccountId">Уникальный идентификатор аккаунта пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    /// <param name="code">Шестизначный MFA-код.</param>
    public UserAccountMfaEmailCodeGeneratedIntegrationEvent(
        DateTimeOffset occurredAt,
        Guid userAccountId,
        string email,
        string code)
        : base(occurredAt, userAccountId, email) => Code = code;

    /// <summary>
    /// Шестизначный MFA-код отправленный на электронную почту.
    /// </summary>
    public string Code { get; }
}
