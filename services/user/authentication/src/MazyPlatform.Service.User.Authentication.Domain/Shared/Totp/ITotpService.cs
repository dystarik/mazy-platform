namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Определяет контракт для создания и верификации TOTP-аутентификатора (RFC 6238).
/// </summary>
/// <remarks>
/// Используется при добавлении TOTP-метода MFA: <see cref="CreateSetup"/> формирует настройку,
/// а <see cref="VerifyCode"/> вызывается из <see cref="VerifyMfaFactorService"/> при каждой
/// попытке подтверждения кода.
/// </remarks>
public interface ITotpService
{
    /// <summary>
    /// Создаёт новую TOTP-настройку для указанного пользователя.
    /// </summary>
    /// <param name="email">Email аккаунта, с которым связывается аутентификатор.</param>
    /// <returns>
    /// <see cref="TotpSetup"/> с секретным ключом (<see cref="TotpPayload"/>)
    /// и URI для QR-кода (<c>otpauth://</c>).
    /// </returns>
    TotpSetup CreateSetup(Email email);

    /// <summary>
    /// Проверяет соответствие TOTP-кода текущему временному окну.
    /// </summary>
    /// <param name="secret">Секретный ключ из <see cref="TotpPayload.Secret"/>.</param>
    /// <param name="code">Шестизначный код, введённый пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если код валиден в текущем или смежном временном окне;
    /// <see langword="false"/> в противном случае.
    /// </returns>
    bool VerifyCode(string secret, string code);
}
