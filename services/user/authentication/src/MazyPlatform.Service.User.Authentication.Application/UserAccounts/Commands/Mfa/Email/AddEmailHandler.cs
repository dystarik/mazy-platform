namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
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
        var mfaEmail = userAccount.Email;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var mfaEmailR = Email.Create(command.Email);
            if (mfaEmailR.IsFailure)
            {
                InvalidEmail(command.Email, mfaEmailR.Errors);
                return mfaEmailR.Errors;
            }

            mfaEmail = mfaEmailR.Value;
        }

        var addMfaMethodR = userAccount.AddMfaMethod(new EmailPayload(mfaEmail), now);
        if (addMfaMethodR.IsFailure)
        {
            AddMfaMethodFailed(mfaEmail.Value, addMfaMethodR.Errors);
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
                ConfirmMfaMethodFailed(mfaEmail.Value, mfaMethodConfirmationR.Errors);
                return mfaMethodConfirmationR.Errors;
            }

            backupCodes = mfaMethodConfirmationR.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        EmailMfaAdded(mfaEmail.Value);
        return new AddEmailResult(otpId, backupCodes, isVerificationRequired);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "UserAccount с ID '{UserAccountId}' не найден, возможно токен недействителен или аккаунт удалён.")]
    private partial void UserAccountNotFound(string userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка добавления Email MFA-метода для пользователя с email '{Email}': {Errors}")]
    private partial void AddMfaMethodFailed(string email, object errors);

    [LoggerMessage(3, LogLevel.Information, "Пользователь успешно добавил Email MFA-метод для email '{Email}'.")]
    private partial void EmailMfaAdded(string email);

    [LoggerMessage(4, LogLevel.Error, "Внутренняя ошибка подтверждения Email MFA-метода для пользователя с email '{Email}': {Errors}")]
    private partial void ConfirmMfaMethodFailed(string email, object errors);

    [LoggerMessage(5, LogLevel.Warning, "Не удалось создать ValueObject Email из строки '{Email}': {Errors}")]
    private partial void InvalidEmail(string email, object errors);
    #endregion
}
