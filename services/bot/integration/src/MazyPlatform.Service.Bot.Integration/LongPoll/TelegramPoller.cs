namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using System.Text.Json;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Configuration.Options;
using MazyPlatform.Service.Bot.Integration.Messaging;

internal sealed partial class TelegramPoller(
    BotInstanceCacheEntry entry,
    BotInstanceCache cache,
    TelegramPollingClient pollingClient,
    IEventPublisher publisher,
    TelegramOptions telegramOptions,
    ILogger<TelegramPoller> logger) : IBotPoller
{
    private long? _offset;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        LogStarting(entry.BotInstanceId);

        while (!cancellationToken.IsCancellationRequested)
        {
            TelegramUpdatesResponse response;

            try
            {
                var currentEntry = GetCurrentEntry();
                response = await pollingClient.GetUpdatesAsync(
                    currentEntry.AccessToken,
                    _offset,
                    telegramOptions.TimeoutSeconds,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogPollError(ex);
                await Task.Delay(TimeSpan.FromSeconds(telegramOptions.RetryDelaySeconds), cancellationToken);
                continue;
            }

            foreach (var update in response.Updates)
            {
                TrackOffset(update);

                if (!IsSupportedUpdate(update))
                    continue;

                await HandleUpdateAsync(update, cancellationToken);
            }
        }
    }

    private static bool IsSupportedUpdate(JsonElement update) =>
        update.TryGetProperty("message", out _)
        || update.TryGetProperty("callback_query", out _);

    private async Task HandleUpdateAsync(JsonElement update, CancellationToken cancellationToken)
    {
        var currentEntry = GetCurrentEntry();
        var @event = new BotIncomingEventIntegrationEvent(
            occurredAt: DateTimeOffset.UtcNow,
            platform: PlatformType.Telegram,
            botId: currentEntry.BotInstanceId,
            projectId: currentEntry.ProjectId,
            scenarioVersion: currentEntry.ScenarioVersion,
            rawPayload: update.GetRawText());

        try
        {
            await publisher.PublishAsync(@event, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPublishError(ex, currentEntry.BotInstanceId);
        }
    }

    private void TrackOffset(JsonElement update)
    {
        if (!update.TryGetProperty("update_id", out var updateIdProperty))
            return;

        var updateId = updateIdProperty.GetInt64();
        var nextOffset = updateId + 1;
        if (_offset is null || nextOffset > _offset)
            _offset = nextOffset;
    }

    private BotInstanceCacheEntry GetCurrentEntry()
    {
        return cache.TryGet(entry.BotInstanceId, out var current) && current is not null
            ? current
            : entry;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Запуск Telegram polling. BotInstanceId: {BotInstanceId}.")]
    private partial void LogStarting(Guid botInstanceId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error,
        Message = "Ошибка Telegram polling запроса. Повтор позже.")]
    private partial void LogPollError(Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Ошибка публикации события. BotInstanceId: {BotInstanceId}.")]
    private partial void LogPublishError(Exception exception, Guid botInstanceId);
}
