namespace MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotInstanceDeactivatedIntegrationEventHandler(
    BotInstanceCache cache,
    BotPollerOrchestrator orchestrator,
    ILogger<BotInstanceDeactivatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceDeactivatedIntegrationEvent>
{
    public Task HandleAsync(BotInstanceDeactivatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        orchestrator.StopPoller(@event.BotInstanceId);
        cache.Remove(@event.BotInstanceId);
        LogStopped(@event.BotInstanceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Бот деактивирован, поллер остановлен. BotInstanceId: {BotInstanceId}.")]
    private partial void LogStopped(Guid botInstanceId);
}
