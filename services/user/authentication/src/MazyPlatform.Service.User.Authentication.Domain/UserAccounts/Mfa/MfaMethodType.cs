namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

/// <summary>
/// Перечисление поддерживаемых типов методов многофакторной аутентификации.
/// </summary>
/// <seealso cref="MfaMethod"/>
/// <seealso cref="IMfaPayload"/>
public enum MfaMethodType
{
    /// <summary>
    /// Временной одноразовый пароль (TOTP, RFC 6238), генерируемый приложением-аутентификатором.
    /// </summary>
    Totp = 1,

    /// <summary>
    /// Код подтверждения, отправляемый на email пользователя.
    /// </summary>
    Email = 2,

    /// <summary>
    /// Резервный код для восстановления доступа при утере основного метода MFA.
    /// </summary>
    BackupCode = 3,
}
