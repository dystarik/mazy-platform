namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.DomainEventHandlers;

using MazyPlatform.Contracts.User.Authentication.Events;
using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;
using MazyPlatform.SharedKernel.Application.Abstractions.Events;

using Microsoft.Extensions.Logging;

internal sealed partial class UserAccountEmailVerifiedHandler(
    IIntegrationEventPublisher eventPublisher,
    ILogger<UserAccountEmailVerifiedHandler> logger) : IDomainEventHandler<UserAccountEmailVerifiedDomainEvent>
{
    public Task HandleAsync(UserAccountEmailVerifiedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var integrationEvent = new UserAccountEmailConfirmedIntegrationEvent(domainEvent.OccurredAt, domainEvent.UserAccountId, domainEvent.Email.Value);
        ConfirmationEmailPublished(domainEvent.UserAccountId, domainEvent.Email.Value);
        return eventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }

    #region Logging
    [LoggerMessage(0, LogLevel.Information, "Успешно опубликовано событие UserAccountEmailConfirmedIntegrationEvent для аккаунта {UserAccountId} с email {Email}.")]
    private partial void ConfirmationEmailPublished(Guid userAccountId, string email);
    #endregion
}
