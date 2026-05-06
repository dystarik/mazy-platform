namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class AddEmailHandler(
    IUserAccountRepository userAccountRepository,
    IOneTimePasswordRepository otpRepository,
    ICodeHasher codeHasher,
    IBackupCodeHasher backupCodeHasher,
    TimeProvider timeProvider,
    ILogger<AddEmailHandler> logger,
    IUnitOfWork unitOfWork) : ICommandHandler<AddEmailCommand, AddEmailResult>
{
    public async Task<Result<AddEmailResult>> HandleAsync(AddEmailCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);
        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(command.UserAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
        }

        var now = timeProvider.GetUtcNow();

        var addMfaMethodR = userAccount.AddMfaMethod(new EmailPayload(userAccount.Email), now);
        if (addMfaMethodR.IsFailure)
        {
            AddMfaMethodFailed(userAccount.Email.Value, addMfaMethodR.Errors);
            return addMfaMethodR.Errors;
        }

        Guid? otpId = null;
        IReadOnlyCollection<BackupCode>? backupCodes = null;
        var isVerificationRequired = addMfaMethodR.Value;
        if (isVerificationRequired)
        {
            var otp = OneTimePassword.Create(userAccountId, OtpType.MfaEmail, codeHasher, now);
            otpRepository.Add(otp);
            otpId = otp.Id;
        }
        else
        {
            var mfaMethodConfirmationR = userAccount.ConfirmMfaMethod(MfaMethodType.Email, backupCodeHasher, now);
            if (mfaMethodConfirmationR.IsFailure)
            {
                ConfirmMfaMethodFailed(userAccount.Email.Value, mfaMethodConfirmationR.Errors);
                return mfaMethodConfirmationR.Errors;
            }

            backupCodes = mfaMethodConfirmationR.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        EmailMfaAdded(userAccount.Email.Value);
        return new AddEmailResult(otpId, backupCodes, isVerificationRequired);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "UserAccount с ID '{UserAccountId}' не найден, возможно токен недействителен или аккаунт удалён.")]
    private partial void UserAccountNotFound(string userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка добавления Email MFA-метода для пользователя с email '{Email}': {Errors}")]
    private partial void AddMfaMethodFailed(string email, object errors);

    [LoggerMessage(3, LogLevel.Information, "Пользователь с email '{Email}' успешно добавил Email MFA-метод.")]
    private partial void EmailMfaAdded(string email);

    [LoggerMessage(4, LogLevel.Error, "Внутренняя ошибка подтверждения Email MFA-метода для пользователя с email '{Email}': {Errors}")]
    private partial void ConfirmMfaMethodFailed(string email, object errors);
    #endregion
}
