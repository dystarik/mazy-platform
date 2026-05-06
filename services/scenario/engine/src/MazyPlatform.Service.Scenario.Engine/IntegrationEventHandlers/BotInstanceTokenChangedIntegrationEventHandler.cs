namespace MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Grpc;

internal sealed partial class BotInstanceTokenChangedIntegrationEventHandler(
    IBotManagerClient botManagerClient,
    BotInstanceCache cache,
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

        var updatedEntry = await botManagerClient.GetBotCacheEntryAsync(
            @event.BotInstanceId,
            existing.ProjectId,
            existing.ScenarioVersion,
            existing.PlatformType,
            cancellationToken);

        if (updatedEntry is null)
        {
            LogCredentialsNotFound(@event.BotInstanceId);
            return;
        }

        cache.Set(@event.BotInstanceId, updatedEntry);

        LogTokenUpdated(@event.BotInstanceId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Бот не найден в кэше, пропуск обновления токена. BotInstanceId: {BotInstanceId}.")]
    private partial void LogNotInCache(Guid botInstanceId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Не удалось получить новые credentials. BotInstanceId: {BotInstanceId}.")]
    private partial void LogCredentialsNotFound(Guid botInstanceId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information,
        Message = "Токен бота обновлён в кэше. BotInstanceId: {BotInstanceId}.")]
    private partial void LogTokenUpdated(Guid botInstanceId);
}
