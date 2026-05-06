namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

internal sealed partial class BotInstanceScenarioVersionChangedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceScenarioVersionChangedDomainEventHandler> logger) : IDomainEventHandler<BotInstanceScenarioVersionChangedDomainEvent>
{
    public async Task HandleAsync(BotInstanceScenarioVersionChangedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new BotInstanceScenarioVersionChangedIntegrationEvent(@event.OccurredAt, @event.BotInstanceId, @event.NewScenarioVersion),
            cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId, @event.NewScenarioVersion);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceScenarioVersionChangedIntegrationEvent для бота '{BotInstanceId}', новая версия: {NewScenarioVersion}.")]
    private partial void IntegrationEventPublished(Guid botInstanceId, int newScenarioVersion);
    #endregion
}
