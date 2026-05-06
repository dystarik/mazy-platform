namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class AddTotpHandler(
    IUserAccountRepository userAccountRepository,
    TimeProvider timeProvider,
    ITotpService totpService,
    ILogger<AddTotpHandler> logger,
    IUnitOfWork unitOfWork) : ICommandHandler<AddTotpCommand, AddTotpResult>
{
    public async Task<Result<AddTotpResult>> HandleAsync(AddTotpCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);
        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(command.UserAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
        }

        var totp = totpService.CreateSetup(userAccount.Email);

        var addMfaMethodR = userAccount.AddMfaMethod(totp.Payload, timeProvider.GetUtcNow());
        if (addMfaMethodR.IsFailure)
        {
            AddMfaMethodFailed(userAccount.Email.Value, addMfaMethodR.Errors);
            return addMfaMethodR.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        TotpAdded(userAccount.Email.Value);
        return new AddTotpResult(totp.ProvisioningUri);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "UserAccount с ID '{UserAccountId}' не найден, возможно токен недействителен или аккаунт удалён.")]
    private partial void UserAccountNotFound(string userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка добавления TOTP MFA-метода для пользователя с email '{Email}': {Errors}")]
    private partial void AddMfaMethodFailed(string email, object errors);

    [LoggerMessage(3, LogLevel.Information, "Пользователь с email '{Email}' успешно добавил TOTP MFA-метод.")]
    private partial void TotpAdded(string email);
    #endregion
}
