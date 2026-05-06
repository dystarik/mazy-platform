namespace MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Grpc;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotInstanceTokenChangedIntegrationEventHandler(
    IBotManagerClient botManagerClient,
    BotInstanceCache cache,
    BotPollerOrchestrator orchestrator,
    ILogger<BotInstanceTokenChangedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceTokenChangedIntegrationEvent>
{
    public async Task HandleAsync(BotInstanceTokenChangedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        if (!cache.TryGet(@event.BotInstanceId, out var existing) || existing is null)
        {
            LogNotInCache(@event.BotInstanceId);
            return;
        }

        var updated = await botManagerClient.GetBotCacheEntryAsync(
            @event.BotInstanceId,
            existing.ProjectId,
            existing.ScenarioVersion,
            existing.PlatformType,
            cancellationToken);

        if (updated is null)
        {
            LogCredentialsNotFound(@event.BotInstanceId);
            return;
        }

        cache.Set(@event.BotInstanceId, updated);

        // Перезапускаем поллер — он держит снимок entry с токеном
        orchestrator.StopPoller(@event.BotInstanceId);
        orchestrator.StartPoller(updated);

        LogTokenRefreshed(@event.BotInstanceId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Бот не найден в кэше, пропуск обновления токена. BotInstanceId: {BotInstanceId}.")]
    private partial void LogNotInCache(Guid botInstanceId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Не удалось получить новые credentials. BotInstanceId: {BotInstanceId}.")]
    private partial void LogCredentialsNotFound(Guid botInstanceId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information,
        Message = "Токен обновлён, поллер перезапущен. BotInstanceId: {BotInstanceId}.")]
    private partial void LogTokenRefreshed(Guid botInstanceId);
}
