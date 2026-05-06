namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменный сервис установки первого локального пароля аутентифицированного пользователя.
/// </summary>
public sealed class SetPasswordService(
    IUserAccountRepository userAccountRepository,
    IMfaSessionRepository mfaSessionRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Устанавливает первый локальный пароль пользователя, при необходимости требуя прохождения MFA.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта пользователя.</param>
    /// <param name="newPassword">Новый открытый пароль.</param>
    /// <param name="mfaSessionId">Идентификатор завершённой MFA-сессии установки пароля; <see langword="null"/> при первичном запросе.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see cref="Result{T}"/> с <see cref="SetPasswordOutput"/>.</returns>
    public async Task<Result<SetPasswordOutput>> ExecuteAsync(Guid userAccountId, Password newPassword, Guid? mfaSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newPassword);

        var now = timeProvider.GetUtcNow();

        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.NotFound, "Пользователь не найден.");

        if (userAccount.HasPassword)
            return Error.Validation(ErrorCodes.Auth.UserAccount.PasswordAlreadySet, "Локальный пароль уже установлен. Используйте смену пароля.");

        if (userAccount.MfaSettings.HasConfirmedMfaMethod)
        {
            if (mfaSessionId is { } id)
            {
                var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(id, userAccountId, cancellationToken);
                if (mfaSession?.Action is not MfaSessionAction.SetPassword || !mfaSession.IsCompleted || mfaSession.ValidUntil < now)
                    return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");
            }
            else
            {
                var availableFactors = userAccount.MfaSettings.AvailableMfaMethodTypes;
                var newMfaSession = MfaSession.Create(userAccountId, MfaSessionAction.SetPassword, availableFactors, now);
                return new SetPasswordOutput(newMfaSession, availableFactors);
            }
        }

        var newPasswordHash = PasswordHash.FromTrusted(passwordHasher.Hash(newPassword.Value));
        var setPasswordR = userAccount.SetPassword(newPasswordHash, now);
        if (setPasswordR.IsFailure)
            return setPasswordR.Errors;

        return new SetPasswordOutput();
    }
}
