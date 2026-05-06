namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

/// <summary>
/// Результат выполнения <see cref="ResetPasswordService.ExecuteAsync"/>.
/// </summary>
/// <remarks>
/// Дискриминированный union: либо требуется прохождение MFA, либо выслан OTP-код.
/// </remarks>
public sealed record ResetPasswordOutput
{
    /// <summary>
    /// Инициализирует результат с OTP-вызовом (для аккаунтов без MFA).
    /// </summary>
    /// <param name="otp">Одноразовый пароль типа <see cref="OtpType.PasswordReset"/>.</param>
    public ResetPasswordOutput(OneTimePassword otp)
    {
        ArgumentNullException.ThrowIfNull(otp);

        RequiresMfa = false;
        OtpChallengeData = new OtpChallenge(otp);
    }

    /// <summary>
    /// Инициализирует результат с MFA-вызовом (для аккаунтов с настроенным MFA).
    /// </summary>
    /// <param name="mfaSession">Новая MFA-сессия с действием <see cref="MfaSessionAction.ResetPassword"/>.</param>
    /// <param name="availableFactors">Доступные типы MFA-методов у аккаунта.</param>
    public ResetPasswordOutput(MfaSession mfaSession, IReadOnlyCollection<MfaMethodType> availableFactors)
    {
        ArgumentNullException.ThrowIfNull(mfaSession);

        RequiresMfa = true;
        MfaChallengeData = new MfaChallenge(mfaSession, availableFactors);
    }

    /// <summary>
    /// Если <see langword="true"/> — требуется прохождение MFA, <see cref="MfaChallengeData"/> не равно <see langword="null"/>.
    /// Если <see langword="false"/> — OTP-код выслан, <see cref="OtpChallengeData"/> не равно <see langword="null"/>.
    /// </summary>
    [MemberNotNullWhen(true, nameof(MfaChallengeData))]
    [MemberNotNullWhen(false, nameof(OtpChallengeData))]
    public bool RequiresMfa { get; init; }

    /// <summary>
    /// Данные OTP-вызова. Не равно <see langword="null"/> только при <see cref="RequiresMfa"/> равном <see langword="false"/>.
    /// </summary>
    public OtpChallenge? OtpChallengeData { get; init; }

    /// <summary>
    /// Данные MFA-вызова. Не равно <see langword="null"/> только при <see cref="RequiresMfa"/> равном <see langword="true"/>.
    /// </summary>
    public MfaChallenge? MfaChallengeData { get; init; }

    /// <summary>
    /// Данные OTP-вызова для сброса пароля без MFA.
    /// </summary>
    /// <param name="Otp">Одноразовый пароль для подтверждения и последующей смены пароля.</param>
    public sealed record OtpChallenge(OneTimePassword Otp);

    /// <summary>
    /// Данные MFA-вызова для сброса пароля с MFA.
    /// </summary>
    /// <param name="MfaSession">Новая MFA-сессия с действием <see cref="MfaSessionAction.ResetPassword"/>.</param>
    /// <param name="AvailableFactors">Доступные типы MFA-методов для отображения пользователю.</param>
    public sealed record MfaChallenge(MfaSession MfaSession, IReadOnlyCollection<MfaMethodType> AvailableFactors);
}
