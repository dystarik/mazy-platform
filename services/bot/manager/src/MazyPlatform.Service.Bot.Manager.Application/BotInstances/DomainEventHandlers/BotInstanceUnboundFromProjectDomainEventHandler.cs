namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

internal sealed partial class BotInstanceUnboundFromProjectDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceUnboundFromProjectDomainEventHandler> logger)
    : IDomainEventHandler<BotInstanceUnboundFromProjectDomainEvent>
{
    public async Task HandleAsync(BotInstanceUnboundFromProjectDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new BotInstanceUnboundFromProjectIntegrationEvent(@event.OccurredAt, @event.BotInstanceId, @event.FormerProjectId),
            cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceUnboundFromProjectIntegrationEvent для бота '{BotInstanceId}'.")]
    private partial void IntegrationEventPublished(Guid botInstanceId);
    #endregion
}
