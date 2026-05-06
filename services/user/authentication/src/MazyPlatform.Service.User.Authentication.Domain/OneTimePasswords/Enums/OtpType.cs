namespace MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;

/// <summary>
/// Тип одноразового пароля, определяющий его назначение и срок действия.
/// </summary>
/// <seealso cref="OneTimePassword"/>
/// <seealso cref="OtpTypeExtensions"/>
public enum OtpType
{
    /// <summary>
    /// Код для подтверждения email при регистрации. Срок действия — 15 минут.
    /// </summary>
    EmailConfirmation = 0,

    /// <summary>
    /// Код для прохождения Email-фактора MFA. Срок действия — 5 минут.
    /// </summary>
    MfaEmail = 1,

    /// <summary>
    /// Код для сброса пароля через email (используется при отсутствии MFA). Срок действия — 15 минут.
    /// </summary>
    PasswordReset = 2,
}
