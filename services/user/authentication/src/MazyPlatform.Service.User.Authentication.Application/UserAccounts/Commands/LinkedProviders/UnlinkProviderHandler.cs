namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.LinkedProviders;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class UnlinkProviderHandler(
    IUserAccountRepository userAccountRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<UnlinkProviderHandler> logger) : ICommandHandler<UnlinkProviderCommand>
{
    public async Task<Result> HandleAsync(UnlinkProviderCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);
        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);

        if (userAccount is null)
        {
            UserAccountNotFound(command.UserAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
        }

        var unlinkR = userAccount.UnlinkProvider(command.ProviderType, timeProvider.GetUtcNow());
        if (unlinkR.IsFailure)
            return unlinkR;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        ProviderUnlinked(command.ProviderType, userAccountId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Error, "Не найден аккаунт пользователя с ID '{UserAccountId}'")]
    private partial void UserAccountNotFound(string userAccountId);

    [LoggerMessage(2, LogLevel.Information, "Провайдер '{ProviderType}' отвязан от аккаунта {UserAccountId}")]
    private partial void ProviderUnlinked(ExternalProviderType providerType, Guid userAccountId);
    #endregion
}
