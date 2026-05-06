namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменный сервис инициирования сброса пароля.
/// </summary>
/// <remarks>
/// Первый шаг flow сброса пароля: принимает email, находит аккаунт и
/// выбирает подходящий канал подтверждения личности (OTP или MFA).
/// </remarks>
/// <seealso cref="ResetPasswordOutput"/>
/// <seealso cref="ConfirmResetPasswordService"/>
public sealed class ResetPasswordService(
    IUserAccountRepository userAccountRepository,
    ICodeHasher codeHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Инициирует сброс пароля по email.
    /// </summary>
    /// <param name="email">Email аккаунта, пароль которого нужно сбросить.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с <see cref="ResetPasswordOutput"/>, где:
    /// — создаётся OTP типа <see cref="OtpType.PasswordReset"/>, если у аккаунта нет настроенного MFA;
    /// — создаётся MFA-сессия с действием <see cref="MfaSessionAction.ResetPassword"/>, если MFA настроен;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.NotFound"/>, если аккаунт не найден.
    /// </returns>
    public async Task<Result<ResetPasswordOutput>> ExecuteAsync(Email email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        var userAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken);
        if (userAccount is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.NotFound, "Пользователь не найден.");

        var now = timeProvider.GetUtcNow();
        if (userAccount.MfaSettings.HasConfirmedMfaMethod)
        {
            var availableFactors = userAccount.MfaSettings.AvailableMfaMethodTypes;
            var mfaSession = MfaSession.Create(userAccount.Id, MfaSessionAction.ResetPassword, availableFactors, now);
            return new ResetPasswordOutput(mfaSession, availableFactors);
        }

        var otp = OneTimePassword.Create(userAccount.Id, OtpType.PasswordReset, codeHasher, now);
        return new ResetPasswordOutput(otp);
    }
}
