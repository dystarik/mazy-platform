namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;

/// <summary>
/// Полезная нагрузка метода TOTP-аутентификации.
/// </summary>
/// <remarks>
/// Содержит общий секретный ключ, который хранится зашифрованным на стороне сервера
/// и используется для верификации TOTP-кодов через <see cref="ITotpService.VerifyCode"/>.
/// Создаётся сервисом <see cref="ITotpService.CreateSetup"/> и передаётся в
/// <see cref="UserAccount.AddMfaMethod"/>.
/// </remarks>
/// <seealso cref="TotpSetup"/>
/// <seealso cref="ITotpService"/>
public sealed record TotpPayload : IMfaPayload
{
    /// <summary>
    /// Инициализирует полезную нагрузку TOTP-метода.
    /// </summary>
    /// <param name="secret">Base32-кодированный общий секретный ключ TOTP.</param>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="secret"/> пустой или состоит из пробелов.
    /// </exception>
    public TotpPayload(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        Secret = secret;
    }

    /// <inheritdoc />
    public MfaMethodType Type => MfaMethodType.Totp;

    /// <summary>
    /// Base32-кодированный общий секретный ключ TOTP.
    /// </summary>
    public string Secret { get; }
}
