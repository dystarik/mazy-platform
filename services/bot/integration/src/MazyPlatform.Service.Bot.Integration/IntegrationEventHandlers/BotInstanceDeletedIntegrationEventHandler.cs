namespace MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotInstanceDeletedIntegrationEventHandler(
    BotInstanceCache cache,
    BotPollerOrchestrator orchestrator,
    ILogger<BotInstanceDeletedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceDeletedIntegrationEvent>
{
    public Task HandleAsync(BotInstanceDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        orchestrator.StopPoller(@event.BotInstanceId);
        cache.Remove(@event.BotInstanceId);
        LogRemoved(@event.BotInstanceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Бот удалён, поллер остановлен. BotInstanceId: {BotInstanceId}.")]
    private partial void LogRemoved(Guid botInstanceId);
}
