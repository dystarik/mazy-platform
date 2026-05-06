namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;

using MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using Microsoft.Extensions.Options;

using OtpNet;

/// <summary>
/// Реализация <see cref="ITotpService"/> на основе библиотеки <c>OtpNet</c>
/// (RFC 6238: HMAC-SHA1, временной шаг 30 секунд, 6 цифр).
/// </summary>
/// <remarks>
/// Окно верификации настраивается через <see cref="TotpOptions.VerificationWindowPastSteps"/> и
/// <see cref="TotpOptions.VerificationWindowFutureSteps"/>, что позволяет компенсировать небольшое
/// расхождение системных часов клиента и сервера.
/// При генерации настройки (<see cref="ITotpService.CreateSetup"/>) секрет создаётся в формате Base32
/// и возвращается в <see cref="TotpPayload"/> вместе с URI для provisioning-QR.
/// </remarks>
internal sealed class TotpService(IOptions<TotpOptions> totpSettings) : ITotpService
{
    private readonly TotpOptions _totpSettings = totpSettings.Value;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> равен <see langword="null"/>.</exception>
    public TotpSetup CreateSetup(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);
        var provisioningUri = new OtpUri(
            OtpType.Totp,
            secret,
            email.Value,
            _totpSettings.Issuer).ToString();

        return new TotpSetup(new TotpPayload(secret), provisioningUri);
    }

    /// <inheritdoc />
    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        byte[] secretBytes;
        try
        {
            secretBytes = Base32Encoding.ToBytes(secret);
        }
        catch
        {
            return false;
        }

        var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
        var window = new VerificationWindow(_totpSettings.VerificationWindowPastSteps, _totpSettings.VerificationWindowFutureSteps);

        return totp.VerifyTotp(code, out _, window);
    }
}
