namespace MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Engine.Caching;

internal sealed partial class BotInstanceUnboundFromProjectIntegrationEventHandler(
    BotInstanceCache cache,
    ILogger<BotInstanceUnboundFromProjectIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceUnboundFromProjectIntegrationEvent>
{
    public Task HandleAsync(BotInstanceUnboundFromProjectIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        cache.Remove(@event.BotInstanceId);
        LogRemoved(@event.BotInstanceId, @event.FormerProjectId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Бот удалён из кэша (отвязка от проекта). BotInstanceId: {BotInstanceId}, FormerProjectId: {FormerProjectId}.")]
    private partial void LogRemoved(Guid botInstanceId, Guid formerProjectId);
}
