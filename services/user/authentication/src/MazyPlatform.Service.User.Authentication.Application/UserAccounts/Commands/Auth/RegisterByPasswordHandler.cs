namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using System.Threading;
using System.Threading.Tasks;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class RegisterByPasswordHandler(
    RegisterByPasswordService registerByPasswordService,
    IUserAccountRepository accountRepository,
    IOneTimePasswordRepository otpRepository,
    IUnitOfWork unitOfWork,
    ILogger<RegisterByPasswordHandler> logger) : ICommandHandler<RegisterByPasswordCommand, RegisterByPasswordResult>
{
    public async Task<Result<RegisterByPasswordResult>> HandleAsync(RegisterByPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var emailR = Email.Create(command.Email);
        if (emailR.IsFailure)
        {
            InvalidEmail(command.Email, emailR.Errors);
            return emailR.Errors;
        }

        var passwordR = Password.Create(command.Password);
        if (passwordR.IsFailure)
        {
            InvalidPassword(passwordR.Errors);
            return passwordR.Errors;
        }

        var registerR = await registerByPasswordService.ExecuteAsync(emailR, passwordR, cancellationToken);
        if (registerR.IsFailure)
        {
            RegistrationFailed(command.Email, registerR.Errors);
            return registerR.Errors;
        }

        var value = registerR.Value;
        accountRepository.Add(value.UserAccount);
        otpRepository.Add(value.Otp);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        RegistrationSucceeded(command.Email);
        return new RegisterByPasswordResult(value.Otp.Id);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось создать ValueObject Email из строки '{Email}': {Errors}")]
    private partial void InvalidEmail(string email, object errors);

    [LoggerMessage(2, LogLevel.Warning, "Не удалось создать ValueObject Password: {Errors}")]
    private partial void InvalidPassword(object errors);

    [LoggerMessage(3, LogLevel.Warning, "Ошибка выполнения доменного сервиса RegisterByPasswordService для пользователя с email '{Email}': {Errors}")]
    private partial void RegistrationFailed(string email, object errors);

    [LoggerMessage(4, LogLevel.Information, "Пользователь с email '{Email}' успешно зарегистрирован. Email ожидает подтверждения.")]
    private partial void RegistrationSucceeded(string email);
    #endregion
}
