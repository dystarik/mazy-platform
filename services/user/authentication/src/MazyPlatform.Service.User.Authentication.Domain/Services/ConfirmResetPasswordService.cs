namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменный сервис подтверждения сброса пароля.
/// </summary>
/// <remarks>
/// Поддерживает два взаимоисключающих пути подтверждения: через OTP-код или через завершённую MFA-сессию.
/// Если переданы данные обоих путей, приоритет отдаётся OTP.
/// </remarks>
public sealed class ConfirmResetPasswordService(
    IUserAccountRepository userAccountRepository,
    IOneTimePasswordRepository otpRepository,
    IMfaSessionRepository mfaSessionRepository,
    ICodeHasher codeHasher,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Подтверждает личность пользователя и устанавливает новый пароль.
    /// </summary>
    /// <param name="newPassword">Новый открытый пароль.</param>
    /// <param name="otpId">Идентификатор OTP-записи для подтверждения без MFA.</param>
    /// <param name="otpCode">Открытый код OTP для верификации.</param>
    /// <param name="mfaSessionId">Идентификатор завершённой MFA-сессии с действием <see cref="MfaSessionAction.ResetPassword"/>.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see cref="Result"/> с результатом подтверждения.</returns>
    public async Task<Result> ExecuteAsync(Password newPassword, Guid? otpId, string? otpCode, Guid? mfaSessionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newPassword);

        var now = timeProvider.GetUtcNow();

        if (otpId is not null && !string.IsNullOrWhiteSpace(otpCode))
            return await ConfirmByOtpAsync(newPassword, otpId.Value, otpCode, now, cancellationToken);

        if (mfaSessionId is not null)
            return await ConfirmByMfaSessionAsync(newPassword, mfaSessionId.Value, now, cancellationToken);

        return Error.Validation(ErrorCodes.Auth.Validation.Invalid, "Не переданы данные для подтверждения сброса пароля.");
    }

    private async Task<Result> ConfirmByOtpAsync(Password newPassword, Guid otpId, string otpCode, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var otp = await otpRepository.GetByIdAsync(otpId, cancellationToken);
        if (otp is null || otp.Type is not OtpType.PasswordReset)
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные данные для сброса пароля.");

        var verifyOtpResult = otp.Verify(codeHasher, otpCode, now);
        if (verifyOtpResult.IsFailure)
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные данные для сброса пароля.");

        var userAccount = await userAccountRepository.GetByIdAsync(otp.UserAccountId, cancellationToken);
        if (userAccount is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.NotFound, "Пользователь не найден.");

        var newPasswordHash = PasswordHash.FromTrusted(passwordHasher.Hash(newPassword.Value));
        return userAccount.ChangePassword(newPasswordHash, now);
    }

    private async Task<Result> ConfirmByMfaSessionAsync(Password newPassword, Guid mfaSessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var mfaSession = await mfaSessionRepository.GetByIdAsync(mfaSessionId, cancellationToken);
        if (mfaSession?.Action is not MfaSessionAction.ResetPassword || !mfaSession.IsCompleted || mfaSession.ValidUntil < now)
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительная MFA-сессия.");

        var userAccount = await userAccountRepository.GetByIdAsync(mfaSession.UserAccountId, cancellationToken);
        if (userAccount is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.NotFound, "Пользователь не найден.");

        var newPasswordHash = PasswordHash.FromTrusted(passwordHasher.Hash(newPassword.Value));
        return userAccount.ChangePassword(newPasswordHash, now);
    }
}
