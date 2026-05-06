namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class LoginByExternalProviderHandler(
    IExternalProviderService externalProviderService,
    LoginByExternalProviderService loginByExternalProviderService,
    IJwtTokenGenerator jwtTokenGenerator,
    IUserAccountRepository userAccountRepository,
    IUserSessionRepository userSessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<LoginByExternalProviderHandler> logger) : ICommandHandler<LoginByExternalProviderCommand, LoginByExternalProviderResult>
{
    public async Task<Result<LoginByExternalProviderResult>> HandleAsync(LoginByExternalProviderCommand command, CancellationToken cancellationToken = default)
    {
        var email = await externalProviderService.GetEmailAsync(command.ProviderType, command.Code, cancellationToken);
        if (email is null)
        {
            ExternalProviderEmailNotResolved(command.ProviderType);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");
        }

        var provider = new ExternalProvider(command.ProviderType, email);
        var loginR = await loginByExternalProviderService.ExecuteAsync(provider, cancellationToken);
        if (loginR.IsFailure)
        {
            ExternalProviderLoginFailed(command.ProviderType, email.Value, loginR.Errors);
            return loginR.Errors;
        }

        var loginValue = loginR.Value;
        var userAccount = loginValue.UserAccount;
        var userSession = loginValue.UserSession;
        var refreshToken = loginValue.RefreshToken;

        var accessToken = jwtTokenGenerator.Generate(userAccount.Id, userSession.RefreshTokenId, email);

        userSessionRepository.Add(userSession);
        if (loginValue.IsNewAccount)
        {
            userAccountRepository.Add(userAccount);
            NewAccountCreated(command.ProviderType, email.Value, userAccount.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        ExternalProviderLoginSucceeded(command.ProviderType, email.Value);
        return new LoginByExternalProviderResult(accessToken, refreshToken);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось получить email от внешнего провайдера '{ProviderType}'.")]
    private partial void ExternalProviderEmailNotResolved(ExternalProviderType providerType);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка выполнения доменного сервиса LoginByExternalProviderService для пользователя с email '{Email}' через провайдера '{ProviderType}': {Errors}")]
    private partial void ExternalProviderLoginFailed(ExternalProviderType providerType, string email, object errors);

    [LoggerMessage(3, LogLevel.Information, "Создан новый пользователь {UserAccountId} при входе через внешнего провайдера '{ProviderType}' для email '{Email}'.")]
    private partial void NewAccountCreated(ExternalProviderType providerType, string email, Guid userAccountId);

    [LoggerMessage(4, LogLevel.Information, "Пользователь с email '{Email}' успешно авторизовался через внешнего провайдера '{ProviderType}'.")]
    private partial void ExternalProviderLoginSucceeded(ExternalProviderType providerType, string email);
    #endregion
}
