namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class ConfirmTotpHandler(
    IUserAccountRepository userAccountRepository,
    ITotpService totpService,
    TimeProvider timeProvider,
    ILogger<ConfirmTotpHandler> logger,
    IUnitOfWork unitOfWork,
    IBackupCodeHasher codeHasher) : ICommandHandler<ConfirmTotpCommand, IReadOnlyCollection<BackupCode>>
{
    public async Task<Result<IReadOnlyCollection<BackupCode>>> HandleAsync(ConfirmTotpCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);
        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(command.UserAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
        }

        var totpMethod = userAccount.MfaSettings.TotpMethod;
        if (totpMethod is null)
        {
            TotpNotConfigured(command.UserAccountId);
            return Error.NotFound(ErrorCodes.Auth.UserAccount.MfaMethodNotFound, "Метод MFA не найден.");
        }

        var isVerifyCode = totpService.VerifyCode(totpMethod.Secret, command.Code);
        if (!isVerifyCode)
        {
            TotpCodeInvalid(userAccount.Email.Value);
            return Error.Unauthorized(ErrorCodes.Auth.UserAccount.TotpCodeInvalid, "Неверный код.");
        }

        var confirmMfaMethodR = userAccount.ConfirmMfaMethod(MfaMethodType.Totp, codeHasher, timeProvider.GetUtcNow());
        if (confirmMfaMethodR.IsFailure)
        {
            TotpConfirmError(command.UserAccountId, confirmMfaMethodR.Errors);
            return confirmMfaMethodR;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        TotpConfirmed(userAccount.Email.Value);

        return confirmMfaMethodR;
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "UserAccount с ID '{UserAccountId}' не найден, возможно токен недействителен или аккаунт удалён.")]
    private partial void UserAccountNotFound(string userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Пользователь с ID '{UserAccountId}' пытается подтвердить TOTP, но метод не настроен.")]
    private partial void TotpNotConfigured(string userAccountId);

    [LoggerMessage(3, LogLevel.Warning, "Пользователь с email '{Email}' ввёл неверный TOTP-код.")]
    private partial void TotpCodeInvalid(string email);

    [LoggerMessage(4, LogLevel.Error, "Ошибка при подтверждении TOTP MFA-метода для UserAccount с ID '{UserAccountId}': {Error}")]
    private partial void TotpConfirmError(string userAccountId, ErrorCollection error);

    [LoggerMessage(5, LogLevel.Information, "Пользователь с email '{Email}' успешно подтвердил TOTP MFA-метод.")]
    private partial void TotpConfirmed(string email);
    #endregion
}
