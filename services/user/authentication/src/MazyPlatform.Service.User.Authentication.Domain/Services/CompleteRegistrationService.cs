namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using System;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;
using MazyPlatform.SharedKernel.Domain.Results;

/// <summary>
/// Доменный сервис завершения регистрации через подтверждение email OTP-кодом.
/// </summary>
public sealed class CompleteRegistrationService(ICodeHasher codeHasher, ITokenHasher tokenHasher, TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Верифицирует OTP-код, подтверждает email аккаунта и создаёт новую сессию.
    /// </summary>
    /// <param name="account">Аккаунт, email которого подтверждается.</param>
    /// <param name="oneTimePassword">Одноразовый пароль типа <see cref="OtpType.EmailConfirmation"/>.</param>
    /// <param name="code">Открытый числовой код, введённый пользователем.</param>
    /// <returns><see cref="Result{T}"/> с <see cref="CompleteRegistrationOutput"/>.</returns>
    public Result<CompleteRegistrationOutput> Execute(UserAccount account, OneTimePassword oneTimePassword, string code)
    {
        var now = timeProvider.GetUtcNow();

        var isVerifiedR = oneTimePassword.Verify(codeHasher, code, now);
        if (isVerifiedR.IsFailure)
            return isVerifiedR.Errors;

        account.VerifyEmail(now);

        var refreshToken = Guid.NewGuid().ToString();
        var refreshTokenHash = RefreshTokenHash.FromTrusted(tokenHasher.Hash(refreshToken));
        var userSession = UserSession.CreateNewSession(account.Id, refreshTokenHash, now);

        return new CompleteRegistrationOutput(userSession, refreshToken);
    }
}
