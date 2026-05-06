namespace MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Engine.Caching;

internal sealed partial class BotInstanceDeactivatedIntegrationEventHandler(
    BotInstanceCache cache,
    ILogger<BotInstanceDeactivatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceDeactivatedIntegrationEvent>
{
    public Task HandleAsync(BotInstanceDeactivatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        cache.Remove(@event.BotInstanceId);
        LogRemoved(@event.BotInstanceId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Бот удалён из кэша (деактивация). BotInstanceId: {BotInstanceId}.")]
    private partial void LogRemoved(Guid botInstanceId);
}
