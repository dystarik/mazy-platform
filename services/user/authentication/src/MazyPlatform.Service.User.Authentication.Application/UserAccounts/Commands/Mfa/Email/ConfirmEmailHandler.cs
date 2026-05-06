namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class ConfirmEmailHandler(
    IOneTimePasswordRepository otpRepository,
    IUserAccountRepository userAccountRepository,
    ICodeHasher codeHasher,
    IBackupCodeHasher backupCodeHasher,
    TimeProvider timeProvider,
    ILogger<ConfirmEmailHandler> logger,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmEmailCommand, IReadOnlyCollection<BackupCode>>
{
    public async Task<Result<IReadOnlyCollection<BackupCode>>> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default)
    {
        var otpId = Guid.Parse(command.OtpId);
        var otp = await otpRepository.GetByIdAsync(otpId, cancellationToken);
        if (otp is null)
        {
            OtpNotFound(command.OtpId);
            return Error.NotFound(ErrorCodes.Auth.OneTimePassword.NotFound, "Одноразовый пароль не найден.");
        }

        if (otp.Type is not OtpType.MfaEmail)
        {
            OtpTypeMismatch(command.OtpId, otp.Type);
            return Error.NotFound(ErrorCodes.Auth.OneTimePassword.NotFound, "Одноразовый пароль не найден.");
        }

        var userAccount = await userAccountRepository.GetByIdAsync(otp.UserAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(otp.UserAccountId);
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");
        }

        if (userAccount.MfaSettings.EmailMethod is null)
        {
            EmailMethodNotConfigured(userAccount.Email.Value);
            return Error.NotFound(ErrorCodes.Auth.UserAccount.MfaMethodNotFound, "Метод MFA не найден.");
        }

        var now = timeProvider.GetUtcNow();

        var verifyR = otp.Verify(codeHasher, command.OtpCode, now);
        if (verifyR.IsFailure)
        {
            OtpCodeVerificationFailed(userAccount.Email.Value);
            return verifyR.Errors;
        }

        var confirmR = userAccount.ConfirmMfaMethod(MfaMethodType.Email, backupCodeHasher, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        EmailMfaConfirmed(userAccount.Email.Value);

        return confirmR;
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не найден OneTimePassword с ID '{OtpId}'.")]
    private partial void OtpNotFound(string otpId);

    [LoggerMessage(2, LogLevel.Warning, "OneTimePassword с ID '{OtpId}' имеет неверный тип '{OtpType}'.")]
    private partial void OtpTypeMismatch(string otpId, OtpType otpType);

    [LoggerMessage(3, LogLevel.Critical, "Критическое нарушение целостности данных: OneTimePassword {OtpId} ссылается на несуществующий UserAccount.")]
    private partial void UserAccountNotFound(Guid otpId);

    [LoggerMessage(4, LogLevel.Warning, "Пользователь с email '{Email}' пытается подтвердить Email MFA, но метод не настроен.")]
    private partial void EmailMethodNotConfigured(string email);

    [LoggerMessage(5, LogLevel.Warning, "Неверный OTP-код при подтверждении Email MFA для пользователя с email '{Email}'")]
    private partial void OtpCodeVerificationFailed(string email);

    [LoggerMessage(6, LogLevel.Information, "Пользователь с email '{Email}' успешно подтвердил Email MFA-метод.")]
    private partial void EmailMfaConfirmed(string email);
    #endregion
}
