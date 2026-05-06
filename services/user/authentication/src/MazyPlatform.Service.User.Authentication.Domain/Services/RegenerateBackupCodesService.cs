namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

/// <summary>
/// Доменный сервис регенерации резервных кодов MFA.
/// </summary>
/// <remarks>
/// Дополнительно проверяет, что MFA-сессия не была завершена через резервный код —
/// это предотвращает «самовосстановление» доступа одним резервным кодом.
/// </remarks>
public sealed class RegenerateBackupCodesService(
    IMfaSessionRepository mfaSessionRepository,
    IUserAccountRepository userAccountRepository,
    IBackupCodeHasher backupCodeHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Генерирует новый набор резервных кодов, аннулируя старые.
    /// </summary>
    /// <param name="mfaSessionId">Идентификатор завершённой MFA-сессии с действием <see cref="MfaSessionAction.GenerateBackupCodes"/>.</param>
    /// <param name="userAccountId">Идентификатор аккаунта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с 10 открытыми <see cref="BackupCode"/> при успехе.
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.NotFound"/>, если MFA-сессия не найдена.
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.Expired"/>, если MFA-сессия истекла или не завершена.
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.PrimaryFactorRequired"/>, если сессия завершена через резервный код.
    /// сбой с кодом <see cref="ErrorCodes.Auth.InternalError"/>, если аккаунт не найден.
    /// </returns>
    public async Task<Result<IReadOnlyCollection<BackupCode>>> ExecuteAsync(Guid mfaSessionId, Guid userAccountId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(mfaSessionId, userAccountId, cancellationToken);
        if (mfaSession is null)
            return Error.NotFound(ErrorCodes.Auth.MfaSession.NotFound, "MFA сессия не найдена.");

        if (mfaSession.Action is not MfaSessionAction.GenerateBackupCodes)
            return Error.Validation(ErrorCodes.Auth.Validation.Invalid, "MFA сессия не предназначена для генерации резервных кодов.");

        if (!mfaSession.IsCompleted || mfaSession.ValidUntil < now)
            return Error.Unauthorized(ErrorCodes.Auth.MfaSession.Expired, "Недействительная MFA-сессия.");

        if (mfaSession.IsCompletedByBackupCode)
            return Error.Unauthorized(ErrorCodes.Auth.MfaSession.PrimaryFactorRequired, "Недостаточно прав для выполнения операции.");

        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");

        return userAccount.GenerateNewBackupCodes(backupCodeHasher, now);
    }
}
