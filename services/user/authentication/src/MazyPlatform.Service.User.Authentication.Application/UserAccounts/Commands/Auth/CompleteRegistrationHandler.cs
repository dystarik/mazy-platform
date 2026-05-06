namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class CompleteRegistrationHandler(
    IOneTimePasswordRepository otpRepository,
    IUserAccountRepository accountRepository,
    CompleteRegistrationService completeRegistrationService,
    IUserSessionRepository sessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    ILogger<CompleteRegistrationHandler> logger,
    IUnitOfWork unitOfWork) : ICommandHandler<CompleteRegistrationCommand, CompleteRegistrationResult>
{
    public async Task<Result<CompleteRegistrationResult>> HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken = default)
    {
        var otpId = Guid.Parse(command.OtpId);
        var otp = await otpRepository.GetByIdAsync(otpId, cancellationToken);
        if (otp is null)
        {
            OneTimePasswordNotFound(command.OtpId);
            return Error.NotFound(ErrorCodes.Auth.OneTimePassword.NotFound, "Не найден одноразовый пароль.");
        }

        var userAccount = await accountRepository.GetByIdAsync(otp.UserAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(otpId, otp.UserAccountId);
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");
        }

        var completeRegistrationR = completeRegistrationService.Execute(userAccount, otp, command.OtpCode);
        if (completeRegistrationR.IsFailure)
        {
            CompleteRegistrationFailed(userAccount.Email.Value, completeRegistrationR.Errors);
            return completeRegistrationR.Errors;
        }

        var userSession = completeRegistrationR.Value.UserSession;
        var refreshToken = completeRegistrationR.Value.RefreshToken;

        sessionRepository.Add(userSession);

        var accessToken = jwtTokenGenerator.Generate(userAccount.Id, userSession.RefreshTokenId, userAccount.Email);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        CompleteRegistrationSucceeded(userAccount.Email.Value);

        return new CompleteRegistrationResult(accessToken, refreshToken);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не найден агрегат OneTimePassword с ID '{OtpId}'.")]
    private partial void OneTimePasswordNotFound(string otpId);

    [LoggerMessage(2, LogLevel.Critical, "Критическое нарушение целостности данных: OneTimePassword {OtpId} ссылается на несуществующий UserAccount {UserAccountId}")]
    private partial void UserAccountNotFound(Guid otpId, Guid userAccountId);

    [LoggerMessage(3, LogLevel.Warning, "Ошибка выполнения доменного сервиса CompleteRegistrationService для пользователя с email '{Email}': {Errors}")]
    private partial void CompleteRegistrationFailed(string email, object errors);

    [LoggerMessage(4, LogLevel.Information, "Пользователь с email '{Email}' успешно завершил регистрацию и получил сессию.")]
    private partial void CompleteRegistrationSucceeded(string email);
    #endregion
}
