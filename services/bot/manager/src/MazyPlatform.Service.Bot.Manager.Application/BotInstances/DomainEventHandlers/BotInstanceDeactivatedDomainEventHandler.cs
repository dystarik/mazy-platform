namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

internal sealed partial class BotInstanceDeactivatedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceDeactivatedDomainEventHandler> logger) : IDomainEventHandler<BotInstanceDeactivatedDomainEvent>
{
    public async Task HandleAsync(BotInstanceDeactivatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(new BotInstanceDeactivatedIntegrationEvent(@event.OccurredAt, @event.BotInstanceId), cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceDeactivatedIntegrationEvent для бота '{BotInstanceId}'.")]
    private partial void IntegrationEventPublished(Guid botInstanceId);
    #endregion
}
