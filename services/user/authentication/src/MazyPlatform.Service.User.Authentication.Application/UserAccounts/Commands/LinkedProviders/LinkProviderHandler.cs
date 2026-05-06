namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.LinkedProviders;

using System;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class LinkProviderHandler(
    IUserAccountRepository userAccountRepository,
    IExternalProviderService externalProviderService,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<LinkProviderHandler> logger) : ICommandHandler<LinkProviderCommand>
{
    public async Task<Result> HandleAsync(LinkProviderCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);
        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);

        if (userAccount is null)
        {
            UserAccountNotFound(command.UserAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
        }

        var email = await externalProviderService.GetEmailAsync(command.ProviderType, command.Code, cancellationToken);
        if (email is null)
        {
            ExternalProviderEmailNotResolved(command.ProviderType);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Неверные учетные данные.");
        }

        var provider = new ExternalProvider(command.ProviderType, email);
        var linkR = userAccount.LinkProvider(provider, timeProvider.GetUtcNow());
        if (linkR.IsFailure)
            return linkR;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        ProviderLinked(command.ProviderType, userAccountId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Error, "Не найден аккаунт пользователя с ID '{UserAccountId}'")]
    private partial void UserAccountNotFound(string userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Не удалось получить email от внешнего провайдера '{ProviderType}'")]
    private partial void ExternalProviderEmailNotResolved(ExternalProviderType providerType);

    [LoggerMessage(3, LogLevel.Information, "Провайдер '{ProviderType}' привязан к аккаунту {UserAccountId}")]
    private partial void ProviderLinked(ExternalProviderType providerType, Guid userAccountId);
    #endregion
}
