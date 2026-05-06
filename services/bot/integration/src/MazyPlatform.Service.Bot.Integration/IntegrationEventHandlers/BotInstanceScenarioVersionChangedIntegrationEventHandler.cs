namespace MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotInstanceScenarioVersionChangedIntegrationEventHandler(
    BotInstanceCache cache,
    ILogger<BotInstanceScenarioVersionChangedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceScenarioVersionChangedIntegrationEvent>
{
    public Task HandleAsync(BotInstanceScenarioVersionChangedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        if (!cache.TryGet(@event.BotInstanceId, out var existing) || existing is null)
        {
            LogNotInCache(@event.BotInstanceId);
            return Task.CompletedTask;
        }

        var updated = existing with { ScenarioVersion = @event.NewScenarioVersion };
        cache.Set(@event.BotInstanceId, updated);

        LogScenarioVersionUpdated(@event.BotInstanceId, @event.NewScenarioVersion);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Бот не найден в кэше, пропуск обновления версии сценария. BotInstanceId: {BotInstanceId}.")]
    private partial void LogNotInCache(Guid botInstanceId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Версия сценария обновлена в кэше. BotInstanceId: {BotInstanceId}, NewScenarioVersion: {NewScenarioVersion}.")]
    private partial void LogScenarioVersionUpdated(Guid botInstanceId, int newScenarioVersion);
}
