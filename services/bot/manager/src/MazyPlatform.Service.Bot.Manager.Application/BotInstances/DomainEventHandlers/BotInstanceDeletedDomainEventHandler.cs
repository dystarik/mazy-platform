namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

internal sealed partial class BotInstanceDeletedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceDeletedDomainEventHandler> logger) : IDomainEventHandler<BotInstanceDeletedDomainEvent>
{
    public async Task HandleAsync(BotInstanceDeletedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new BotInstanceDeletedIntegrationEvent(@event.OccurredAt, @event.BotInstanceId, @event.ProjectId),
            cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceDeletedIntegrationEvent для бота '{BotInstanceId}'.")]
    private partial void IntegrationEventPublished(Guid botInstanceId);
    #endregion
}
