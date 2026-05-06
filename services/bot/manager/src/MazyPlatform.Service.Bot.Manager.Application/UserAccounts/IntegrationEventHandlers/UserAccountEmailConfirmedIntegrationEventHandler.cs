namespace MazyPlatform.Service.Bot.Manager.Application.UserAccounts.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;

internal sealed partial class UserAccountEmailConfirmedIntegrationEventHandler(
    ILogger<UserAccountEmailConfirmedIntegrationEventHandler> logger,
    IUserAccountRepository userAccountRepository,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<UserAccountEmailConfirmedIntegrationEvent>
{
    public async Task HandleAsync(UserAccountEmailConfirmedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var existing = await userAccountRepository.GetByIdAsync(@event.UserAccountId, cancellationToken);

        if (existing is not null)
        {
            UserAccountAlreadyExists(@event.UserAccountId);
            return;
        }

        var account = UserAccount.Create(@event.UserAccountId, DateTimeOffset.UtcNow);
        userAccountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        UserAccountCreated(@event.UserAccountId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Аккаунт пользователя '{UserAccountId}' создан.")]
    private partial void UserAccountCreated(Guid userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Аккаунт пользователя '{UserAccountId}' уже существует, пропускаем.")]
    private partial void UserAccountAlreadyExists(Guid userAccountId);
    #endregion
}
