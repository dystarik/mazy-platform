namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

internal sealed partial class BotInstanceTokenChangedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceTokenChangedDomainEventHandler> logger) : IDomainEventHandler<BotInstanceTokenChangedDomainEvent>
{
    public async Task HandleAsync(BotInstanceTokenChangedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(new BotInstanceTokenChangedIntegrationEvent(@event.OccurredAt, @event.BotInstanceId), cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceTokenChangedIntegrationEvent для бота '{BotInstanceId}'.")]
    private partial void IntegrationEventPublished(Guid botInstanceId);
    #endregion
}
