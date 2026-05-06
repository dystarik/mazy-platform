namespace MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Engine.Caching;

internal sealed partial class BotInstanceDeletedIntegrationEventHandler(
    BotInstanceCache cache,
    ILogger<BotInstanceDeletedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceDeletedIntegrationEvent>
{
    public Task HandleAsync(BotInstanceDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        cache.Remove(@event.BotInstanceId);
        LogRemoved(@event.BotInstanceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Бот удалён из кэша (удаление). BotInstanceId: {BotInstanceId}.")]
    private partial void LogRemoved(Guid botInstanceId);
}
