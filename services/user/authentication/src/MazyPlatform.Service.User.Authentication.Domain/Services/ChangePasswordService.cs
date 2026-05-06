namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменный сервис смены пароля аутентифицированного пользователя.
/// </summary>
public sealed class ChangePasswordService(
    IUserAccountRepository userAccountRepository,
    IMfaSessionRepository mfaSessionRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Меняет пароль пользователя, при необходимости требуя прохождения MFA.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта пользователя.</param>
    /// <param name="currentPassword">Текущий открытый пароль для подтверждения личности.</param>
    /// <param name="newPassword">Новый открытый пароль.</param>
    /// <param name="mfaSessionId">Идентификатор завершённой MFA-сессии смены пароля; <see langword="null"/> при первичном запросе.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see cref="Result{T}"/> с <see cref="ChangePasswordOutput"/>.</returns>
    public async Task<Result<ChangePasswordOutput>> ExecuteAsync(Guid userAccountId, Password currentPassword, Password newPassword, Guid? mfaSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);
        ArgumentNullException.ThrowIfNull(newPassword);

        var now = timeProvider.GetUtcNow();

        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount?.HasPassword != true || !passwordHasher.Verify(currentPassword.Value, userAccount.PasswordHash!.Value))
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");

        if (userAccount.MfaSettings.HasConfirmedMfaMethod)
        {
            if (mfaSessionId is { } id)
            {
                var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(id, userAccountId, cancellationToken);
                if (mfaSession?.Action is not MfaSessionAction.ChangePassword || !mfaSession.IsCompleted || mfaSession.ValidUntil < now)
                    return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");
            }
            else
            {
                var availableFactors = userAccount.MfaSettings.AvailableMfaMethodTypes;
                var newMfaSession = MfaSession.Create(userAccountId, MfaSessionAction.ChangePassword, availableFactors, now);
                return new ChangePasswordOutput(newMfaSession, availableFactors);
            }
        }

        var newPasswordHash = PasswordHash.FromTrusted(passwordHasher.Hash(newPassword.Value));
        userAccount.ChangePassword(newPasswordHash, now);

        return new ChangePasswordOutput();
    }
}
