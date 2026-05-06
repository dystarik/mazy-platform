namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class ResendConfirmationCodeHandler(
    IUserAccountRepository userAccountRepository,
    IOneTimePasswordRepository otpRepository,
    ICodeHasher codeHasher,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<ResendConfirmationCodeHandler> logger) : ICommandHandler<ResendConfirmationCodeCommand, ResendConfirmationCodeResult>
{
    public async Task<Result<ResendConfirmationCodeResult>> HandleAsync(ResendConfirmationCodeCommand command, CancellationToken cancellationToken = default)
    {
        var emailR = Email.Create(command.Email);
        if (emailR.IsFailure)
        {
            InvalidEmail(command.Email, emailR.Errors);
            return emailR.Errors;
        }

        var userAccount = await userAccountRepository.GetByUnverifiedEmailAsync(emailR.Value, cancellationToken);
        if (userAccount is null)
        {
            UnverifiedUserAccountNotFound(emailR.Value.Value);
            return Error.NotFound(ErrorCodes.Auth.UserAccount.NotFound, "Пользователь с неподтверждённым email не найден.");
        }

        var now = timeProvider.GetUtcNow();

        var newOtp = OneTimePassword.Create(userAccount.Id, OtpType.EmailConfirmation, codeHasher, now);
        otpRepository.Add(newOtp);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ConfirmationCodeResent(userAccount.Id);
        return new ResendConfirmationCodeResult(newOtp.Id);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось создать ValueObject Email из строки '{Email}': {Errors}")]
    private partial void InvalidEmail(string email, object errors);

    [LoggerMessage(2, LogLevel.Warning, "Пользователь с неподтверждённым email '{Email}' не найден.")]
    private partial void UnverifiedUserAccountNotFound(string email);

    [LoggerMessage(3, LogLevel.Information, "Повторно отправлен код подтверждения для пользователя {UserAccountId}")]
    private partial void ConfirmationCodeResent(Guid userAccountId);
    #endregion
}
