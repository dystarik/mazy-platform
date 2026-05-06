namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

/// <summary>
/// Доменный сервис верификации одного фактора MFA в рамках активной MFA-сессии.
/// </summary>
/// <remarks>
/// Выбирает стратегию верификации на основе <see cref="MfaMethodType"/>:
/// TOTP проверяется через <see cref="ITotpService"/>, Email — через последний активный OTP,
/// резервный код — через <see cref="UserAccount.UseBackupCode"/>.
/// </remarks>
public sealed class VerifyMfaFactorService(
    IUserAccountRepository userAccountRepository,
    IOneTimePasswordRepository otpRepository,
    ITotpService totpService,
    ICodeHasher codeHasher,
    IBackupCodeHasher backupCodeHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Верифицирует введённый код фактора и обновляет состояние MFA-сессии.
    /// </summary>
    /// <param name="session">Активная MFA-сессия, в рамках которой проходит верификация.</param>
    /// <param name="factorType">Тип верифицируемого фактора MFA.</param>
    /// <param name="code">Введённый пользователем код (TOTP, OTP по email или резервный код).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с <see cref="VerifyMfaFactorOutput"/> при успехе;
    /// сбой, если код неверен, истёк, сессия завершена/истекла или метод не найден.
    /// </returns>
    public async Task<Result<VerifyMfaFactorOutput>> ExecuteAsync(MfaSession session, MfaMethodType factorType, string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var now = timeProvider.GetUtcNow();

        var userAccount = await userAccountRepository.GetByIdAsync(session.UserAccountId, cancellationToken);
        if (userAccount is null)
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");

        var verifyR = factorType switch
        {
            MfaMethodType.Totp => VerifyTotp(userAccount, session, code, now),
            MfaMethodType.BackupCode => VerifyBackupCode(userAccount, session, code, now),
            MfaMethodType.Email => await VerifyEmailAsync(userAccount, session, code, now, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(factorType)),
        };

        if (verifyR.IsFailure)
            return verifyR.Errors;

        return new VerifyMfaFactorOutput(session.IsCompleted);
    }

    private Result VerifyTotp(UserAccount userAccount, MfaSession session, string code, DateTimeOffset now)
    {
        var totpMethod = userAccount.MfaSettings.TotpMethod;
        if (totpMethod is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.MfaMethodNotFound, "Метод MFA не найден.");

        var isValid = totpService.VerifyCode(totpMethod.Secret, code);
        if (!isValid)
            return Error.Unauthorized(ErrorCodes.Auth.UserAccount.TotpCodeInvalid, "Неверный код.");

        return session.CompleteWithFactor(MfaMethodType.Totp, now);
    }

    private Result VerifyBackupCode(UserAccount userAccount, MfaSession session, string code, DateTimeOffset now)
    {
        var backupCode = BackupCode.Create(code);
        if (backupCode.IsFailure)
            return Error.Validation(ErrorCodes.Auth.UserAccount.BackupCodeInvalid, "Неверный формат кода резервного доступа.");

        var useBackupCodeR = userAccount.MfaSettings.UseBackupCode(backupCode, backupCodeHasher, now);
        if (useBackupCodeR.IsFailure)
            return Error.Unauthorized(ErrorCodes.Auth.UserAccount.BackupCodeInvalid, "Неверный код резервного доступа.");

        return session.CompleteWithFactor(MfaMethodType.BackupCode, now);
    }

    private async Task<Result> VerifyEmailAsync(UserAccount userAccount, MfaSession session, string code, DateTimeOffset now, CancellationToken ct)
    {
        var emailMethod = userAccount.MfaSettings.EmailMethod;
        if (emailMethod is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.MfaMethodNotFound, "Метод MFA не найден.");

        var otp = await otpRepository.GetLatestUnverifiedByUserAccountIdAsync(session.UserAccountId, OtpType.MfaEmail, ct);

        if (otp is null)
            return Error.NotFound(ErrorCodes.Auth.OneTimePassword.NotFound, "Код подтверждения не найден.");

        var verifyOtpR = otp.Verify(codeHasher, code, now);
        return verifyOtpR.IsFailure ? verifyOtpR : session.CompleteWithFactor(MfaMethodType.Email, now);
    }
}
