namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Configuration.Options;
using MazyPlatform.Service.Bot.Integration.Messaging;

/// <summary>
/// Реализует цикл VK Long Poll для одного бота.
/// Создаётся и управляется через <see cref="BotPollerOrchestrator"/>.
/// </summary>
internal sealed partial class VkPoller(
    BotInstanceCacheEntry entry,
    BotInstanceCache cache,
    VkLongPollClient longPollClient,
    IEventPublisher publisher,
    VkOptions vkOptions,
    ILogger<VkPoller> logger) : IBotPoller
{
    private bool _longPollSettingsEnsured;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        LogStarting(entry.BotInstanceId, entry.RequiredVkCommunityId);

        var server = await GetServerWithRetryAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            LongPollResponse response;

            try
            {
                response = await longPollClient.PollAsync(server, vkOptions.WaitSeconds, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogPollError(ex);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                server = await GetServerWithRetryAsync(cancellationToken);
                continue;
            }

            if (response.Failed is not null)
            {
                server = await HandleFailedAsync(response, server, cancellationToken);
                continue;
            }

            foreach (var update in response.Updates)
                await HandleUpdateAsync(update, cancellationToken);

            server = server with { Ts = response.Ts! };
        }
    }

    private bool IsSupportedUpdateType(string? updateType) =>
        string.Equals(updateType, "message_new", StringComparison.Ordinal)
        || string.Equals(updateType, "message_event", StringComparison.Ordinal);

    private async Task HandleUpdateAsync(System.Text.Json.JsonElement update, CancellationToken cancellationToken)
    {
        if (!update.TryGetProperty("type", out var typeProp))
            return;

        if (!IsSupportedUpdateType(typeProp.GetString()))
            return;

        var currentEntry = GetCurrentEntry();
        var rawPayload = update.GetRawText();
        var answerTask = string.Equals(typeProp.GetString(), "message_event", StringComparison.Ordinal)
            ? AnswerMessageEventAsync(update, currentEntry, cancellationToken)
            : Task.CompletedTask;

        var @event = new BotIncomingEventIntegrationEvent(
            occurredAt: DateTimeOffset.UtcNow,
            platform: PlatformType.Vk,
            botId: currentEntry.BotInstanceId,
            projectId: currentEntry.ProjectId,
            scenarioVersion: currentEntry.ScenarioVersion,
            rawPayload: rawPayload);

        try
        {
            await publisher.PublishAsync(@event, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPublishError(ex, currentEntry.BotInstanceId);
        }

        await answerTask;
    }

    private async Task AnswerMessageEventAsync(
        System.Text.Json.JsonElement update,
        BotInstanceCacheEntry currentEntry,
        CancellationToken cancellationToken)
    {
        var eventObject = update.GetProperty("object");

        if (!eventObject.TryGetProperty("event_id", out var eventIdElement)
            || !eventObject.TryGetProperty("user_id", out var userIdElement)
            || !eventObject.TryGetProperty("peer_id", out var peerIdElement))
        {
            return;
        }

        var eventId = eventIdElement.GetString();

        if (string.IsNullOrEmpty(eventId))
            return;

        try
        {
            await longPollClient.SendMessageEventAnswerAsync(
                eventId,
                userIdElement.GetInt64(),
                peerIdElement.GetInt64(),
                currentEntry.AccessToken,
                vkOptions.ApiVersion,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCallbackAnswerError(ex, currentEntry.BotInstanceId);
        }
    }

    private async Task<LongPollServer> HandleFailedAsync(
        LongPollResponse response,
        LongPollServer current,
        CancellationToken cancellationToken)
    {
        LogLongPollFailed(response.Failed!.Value);

        // failed=1 — история устарела, просто обновляем ts
        if (response.Failed == 1 && response.Ts is not null)
            return current with { Ts = response.Ts };

        // failed=2 или 3 — нужно заново получить сервер
        return await GetServerWithRetryAsync(cancellationToken);
    }

    private async Task<LongPollServer> GetServerWithRetryAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var currentEntry = GetCurrentEntry();
                await EnsureLongPollSettingsAsync(currentEntry, cancellationToken);

                var server = await longPollClient.GetServerAsync(
                    currentEntry.RequiredVkCommunityId,
                    currentEntry.AccessToken,
                    vkOptions.ApiVersion,
                    cancellationToken);

                LogServerObtained(currentEntry.RequiredVkCommunityId);
                return server;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogGetServerError(ex, GetCurrentEntry().RequiredVkCommunityId);
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("Unreachable.");
    }

    private async Task EnsureLongPollSettingsAsync(
        BotInstanceCacheEntry currentEntry,
        CancellationToken cancellationToken)
    {
        if (_longPollSettingsEnsured)
            return;

        try
        {
            await longPollClient.EnsureLongPollSettingsAsync(
                currentEntry.RequiredVkCommunityId,
                currentEntry.AccessToken,
                vkOptions.ApiVersion,
                cancellationToken);

            _longPollSettingsEnsured = true;
            LogLongPollSettingsEnsured(currentEntry.RequiredVkCommunityId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogLongPollSettingsEnsureError(ex, currentEntry.RequiredVkCommunityId);
        }
    }

    private BotInstanceCacheEntry GetCurrentEntry()
    {
        return cache.TryGet(entry.BotInstanceId, out var current) && current is not null
            ? current
            : entry;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Запуск VK Long Poll. BotInstanceId: {BotInstanceId}, GroupId: {GroupId}.")]
    private partial void LogStarting(Guid botInstanceId, int groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Long Poll сервер получен. GroupId: {GroupId}.")]
    private partial void LogServerObtained(int groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Long Poll вернул failed={Failed}. Переподключение.")]
    private partial void LogLongPollFailed(int failed);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error,
        Message = "Ошибка Long Poll запроса. Повтор через 5 сек.")]
    private partial void LogPollError(Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error,
        Message = "Ошибка получения Long Poll сервера. GroupId: {GroupId}. Повтор через 10 сек.")]
    private partial void LogGetServerError(Exception exception, int groupId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error,
        Message = "Ошибка публикации события. BotInstanceId: {BotInstanceId}.")]
    private partial void LogPublishError(Exception exception, Guid botInstanceId);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information,
        Message = "Настройки VK Long Poll обновлены. GroupId: {GroupId}.")]
    private partial void LogLongPollSettingsEnsured(int groupId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning,
        Message = "Не удалось обновить настройки VK Long Poll. GroupId: {GroupId}.")]
    private partial void LogLongPollSettingsEnsureError(Exception exception, int groupId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning,
        Message = "Не удалось ответить на VK message_event. BotInstanceId: {BotInstanceId}.")]
    private partial void LogCallbackAnswerError(Exception exception, Guid botInstanceId);
}
