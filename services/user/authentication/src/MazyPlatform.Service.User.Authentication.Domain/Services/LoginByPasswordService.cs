namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

/// <summary>
/// Доменный сервис входа пользователя по паролю.
/// </summary>
/// <remarks>
/// Возвращает отдельную ошибку для неподтверждённого email,
/// чтобы клиент мог сообщить пользователю о необходимости подтверждения.
/// </remarks>
public sealed class LoginByPasswordService(
    IUserAccountRepository userAccountRepository,
    IMfaSessionRepository mfaSessionRepository,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Аутентифицирует пользователя по паролю, при необходимости инициируя MFA-проверку.
    /// </summary>
    /// <param name="email">Email аккаунта.</param>
    /// <param name="password">Открытый пароль.</param>
    /// <param name="mfaSessionId">
    /// Идентификатор завершённой MFA-сессии входа, если клиент уже прошёл MFA;
    /// <see langword="null"/> при первичной попытке входа.
    /// </param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с <see cref="LoginByPasswordOutput"/>, где:
    /// — <see cref="LoginByPasswordOutput.RequiresMfa"/> равно <see langword="true"/> и создана новая MFA-сессия, если аккаунт имеет MFA и <paramref name="mfaSessionId"/> не передан.
    /// — <see cref="LoginByPasswordOutput.RequiresMfa"/> равно <see langword="false"/> и создана новая <see cref="UserSession"/> при успешном входе.
    /// сбой с кодом <see cref="ErrorCodes.Auth.Unauthorized"/>, если учётные данные неверны или MFA-сессия недействительна.
    /// сбой с кодом <see cref="ErrorCodes.Auth.EmailNotVerified"/> и сообщением о необходимости подтверждения email, если аккаунт не подтверждён.
    /// </returns>
    public async Task<Result<LoginByPasswordOutput>> ExecuteAsync(Email email, Password password, Guid? mfaSessionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        var userAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken);
        if (userAccount?.IsEmailVerified is false)
            return Error.Conflict(ErrorCodes.Auth.EmailVerificationPending, "Email уже зарегистрирован, ожидается подтверждение.");

        if (userAccount?.HasPassword != true || !passwordHasher.Verify(password.Value, userAccount.PasswordHash!.Value))
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");

        var now = timeProvider.GetUtcNow();
        if (userAccount.MfaSettings.HasConfirmedMfaMethod)
        {
            if (mfaSessionId is { } id)
            {
                var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(id, userAccount.Id, cancellationToken);
                if (mfaSession?.Action is not MfaSessionAction.Login || !mfaSession.IsCompleted || mfaSession.ValidUntil < now)
                    return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");
            }
            else
            {
                var availableMfaMethodTypes = userAccount.MfaSettings.AvailableMfaMethodTypes;
                var newMfaSession = MfaSession.Create(userAccount.Id, MfaSessionAction.Login, availableMfaMethodTypes, now);
                return new LoginByPasswordOutput(newMfaSession, availableMfaMethodTypes);
            }
        }

        var refreshToken = Guid.NewGuid().ToString();
        var refreshTokenHash = RefreshTokenHash.FromTrusted(tokenHasher.Hash(refreshToken));
        var userSession = UserSession.CreateNewSession(userAccount.Id, refreshTokenHash, now);

        return new LoginByPasswordOutput(userAccount, userSession, refreshToken);
    }
}
