namespace MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Grpc;
using MazyPlatform.Service.Scenario.Engine.Platforms;

internal sealed partial class BotInstanceActivatedIntegrationEventHandler(
    IBotManagerClient botManagerClient,
    BotInstanceCache cache,
    ILogger<BotInstanceActivatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceActivatedIntegrationEvent>
{
    public async Task HandleAsync(BotInstanceActivatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        if (!ScenarioPlatformMapper.IsSupported(@event.PlatformType))
        {
            LogUnsupportedPlatform(@event.BotInstanceId, @event.PlatformType.ToString());
            return;
        }

        var entry = await botManagerClient.GetBotCacheEntryAsync(
            @event.BotInstanceId,
            @event.ProjectId,
            @event.ScenarioVersion,
            @event.PlatformType,
            cancellationToken);

        if (entry is null)
        {
            LogCredentialsNotFound(@event.BotInstanceId);
            return;
        }

        cache.Set(@event.BotInstanceId, entry);

        LogCached(@event.BotInstanceId, @event.ProjectId, @event.ScenarioVersion);
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Неподдерживаемая платформа. BotInstanceId: {BotInstanceId}, Platform: {Platform}.")]
    private partial void LogUnsupportedPlatform(Guid botInstanceId, string platform);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Не удалось получить credentials. BotInstanceId: {BotInstanceId}.")]
    private partial void LogCredentialsNotFound(Guid botInstanceId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Бот добавлен в кэш. BotInstanceId: {BotInstanceId}, ProjectId: {ProjectId}, Version: {Version}.")]
    private partial void LogCached(Guid botInstanceId, Guid projectId, int version);
    #endregion
}
