namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class LoginByPasswordHandler(
    LoginByPasswordService loginByPasswordService,
    IUserSessionRepository userSessionRepository,
    IMfaSessionRepository mfaSessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    ILogger<LoginByPasswordHandler> logger,
    IUnitOfWork unitOfWork) : ICommandHandler<LoginByPasswordCommand, LoginByPasswordResult>
{
    public async Task<Result<LoginByPasswordResult>> HandleAsync(LoginByPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var emailR = Email.Create(command.Email);
        if (emailR.IsFailure)
            return emailR.Errors;

        var passwordR = Password.Create(command.Password);
        if (passwordR.IsFailure)
            return passwordR.Errors;

        Guid? mfaSessionId = string.IsNullOrWhiteSpace(command.MfaSessionId) ? null : Guid.Parse(command.MfaSessionId);
        var loginR = await loginByPasswordService.ExecuteAsync(emailR, passwordR, mfaSessionId, cancellationToken);
        if (loginR.IsFailure)
        {
            LoginByPasswordFailed(command.Email, loginR.Errors);
            return loginR.Errors;
        }

        var result = loginR.Value.RequiresMfa switch
        {
            true => HandleMfaRequired(loginR.Value.MfaRequiredData),
            false => HandleAuthenticationSuccess(loginR.Value.SuccessData),
        };

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private LoginByPasswordResult HandleAuthenticationSuccess(LoginByPasswordOutput.Success successData)
    {
        var userSession = successData.UserSession;
        var refreshToken = successData.RefreshToken;
        var userAccount = successData.UserAccount;

        userSessionRepository.Add(userSession);

        var accessToken = jwtTokenGenerator.Generate(userAccount.Id, userSession.RefreshTokenId, userAccount.Email);

        LoginSucceeded(userAccount.Email.Value);
        return new LoginByPasswordResult(accessToken, refreshToken);
    }

    private LoginByPasswordResult HandleMfaRequired(LoginByPasswordOutput.MfaRequired mfaRequiredData)
    {
        mfaSessionRepository.Add(mfaRequiredData.MfaSession);

        var mfaSession = mfaRequiredData.MfaSession;
        MfaChallengeIssued(mfaRequiredData.MfaSession.UserAccountId, mfaRequiredData.MfaSession.Id);
        return new LoginByPasswordResult(mfaSession.Id, mfaSession.RequiredFactorCount, mfaRequiredData.AvailableFactor);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Ошибка выполнения доменного сервиса LoginByPasswordService для пользователя с email '{Email}': {Errors}")]
    private partial void LoginByPasswordFailed(string email, object errors);

    [LoggerMessage(2, LogLevel.Information, "Пользователь с email '{Email}' успешно авторизовался и получил сессию.")]
    private partial void LoginSucceeded(string email);

    [LoggerMessage(3, LogLevel.Information, "Для пользователя {UserAccountId} требуется MFA-верификация. Создана MFA сессия {MfaSessionId}.")]
    private partial void MfaChallengeIssued(Guid userAccountId, Guid mfaSessionId);
    #endregion
}
