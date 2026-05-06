namespace MazyPlatform.Service.Bot.Integration.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Grpc;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotInstanceActivatedIntegrationEventHandler(
    IBotManagerClient botManagerClient,
    BotInstanceCache cache,
    BotPollerOrchestrator orchestrator,
    ILogger<BotInstanceActivatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotInstanceActivatedIntegrationEvent>
{
    public async Task HandleAsync(BotInstanceActivatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.PlatformType is not (PlatformType.Vk or PlatformType.Telegram))
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
        orchestrator.StartPoller(entry);

        LogStarted(@event.BotInstanceId, @event.ProjectId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Неподдерживаемая платформа. BotInstanceId: {BotInstanceId}, Platform: {Platform}.")]
    private partial void LogUnsupportedPlatform(Guid botInstanceId, string platform);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Не удалось получить credentials. BotInstanceId: {BotInstanceId}.")]
    private partial void LogCredentialsNotFound(Guid botInstanceId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information,
        Message = "Бот активирован, поллер запущен. BotInstanceId: {BotInstanceId}, ProjectId: {ProjectId}.")]
    private partial void LogStarted(Guid botInstanceId, Guid projectId);
}
