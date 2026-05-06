namespace MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotInstanceUnboundFromProjectIntegrationEventHandler(
    BotInstanceCache cache,
    BotPollerOrchestrator orchestrator,
    ILogger<BotInstanceUnboundFromProjectIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceUnboundFromProjectIntegrationEvent>
{
    public Task HandleAsync(BotInstanceUnboundFromProjectIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        orchestrator.StopPoller(@event.BotInstanceId);
        cache.Remove(@event.BotInstanceId);
        LogStopped(@event.BotInstanceId, @event.FormerProjectId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Бот отвязан от проекта, поллер остановлен. BotInstanceId: {BotInstanceId}, FormerProjectId: {FormerProjectId}.")]
    private partial void LogStopped(Guid botInstanceId, Guid formerProjectId);
}
