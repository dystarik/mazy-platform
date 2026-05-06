namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using System.Collections.Concurrent;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Configuration.Options;
using MazyPlatform.Service.Bot.Integration.Messaging;

using Microsoft.Extensions.Options;

/// <summary>
/// Управляет жизненным циклом platform-specific poller'ов — по одному на каждого активного бота.
/// </summary>
internal sealed partial class BotPollerOrchestrator(
    VkLongPollClient longPollClient,
    TelegramPollingClient telegramPollingClient,
    IEventPublisher publisher,
    BotInstanceCache cache,
    IOptions<VkOptions> vkOptions,
    IOptions<TelegramOptions> telegramOptions,
    ILoggerFactory loggerFactory,
    ILogger<BotPollerOrchestrator> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pollers = new();
    private readonly CancellationTokenSource _globalCts = new();
    private readonly VkOptions _vkOptions = vkOptions.Value;
    private readonly TelegramOptions _telegramOptions = telegramOptions.Value;

    public void StartPoller(BotInstanceCacheEntry entry)
    {
        var poller = CreatePoller(entry);
        if (poller is null)
        {
            LogUnsupportedPlatform(entry.BotInstanceId, entry.PlatformType.ToString());
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);

        if (!_pollers.TryAdd(entry.BotInstanceId, cts))
        {
            cts.Dispose();
            LogAlreadyRunning(entry.BotInstanceId);
            return;
        }

        _ = RunPollerAsync(poller, entry.BotInstanceId, cts.Token);

        LogStarted(entry.BotInstanceId, entry.PlatformType.ToString());
    }

    public void StopPoller(Guid botInstanceId)
    {
        if (!_pollers.TryRemove(botInstanceId, out var cts))
            return;

        cts.Cancel();
        cts.Dispose();

        LogStopped(botInstanceId);
    }

    public void Dispose()
    {
        _globalCts.Cancel();

        foreach (var cts in _pollers.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _pollers.Clear();
        _globalCts.Dispose();
    }

    private IBotPoller? CreatePoller(BotInstanceCacheEntry entry) => entry.PlatformType switch
    {
        PlatformType.Vk => new VkPoller(
            entry,
            cache,
            longPollClient,
            publisher,
            _vkOptions,
            loggerFactory.CreateLogger<VkPoller>()),
        PlatformType.Telegram => new TelegramPoller(
            entry,
            cache,
            telegramPollingClient,
            publisher,
            _telegramOptions,
            loggerFactory.CreateLogger<TelegramPoller>()),
        _ => null,
    };

    private async Task RunPollerAsync(IBotPoller poller, Guid botInstanceId, CancellationToken cancellationToken)
    {
        try
        {
            await poller.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // нормальная остановка
        }
        catch (Exception ex)
        {
            LogPollerFailed(ex, botInstanceId);
        }
        finally
        {
            _pollers.TryRemove(botInstanceId, out _);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Поллер запущен. BotInstanceId: {BotInstanceId}, Platform: {Platform}.")]
    private partial void LogStarted(Guid botInstanceId, string platform);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Поллер остановлен. BotInstanceId: {BotInstanceId}.")]
    private partial void LogStopped(Guid botInstanceId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Поллер уже запущен для бота. BotInstanceId: {BotInstanceId}.")]
    private partial void LogAlreadyRunning(Guid botInstanceId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error,
        Message = "Поллер завершился с ошибкой. BotInstanceId: {BotInstanceId}.")]
    private partial void LogPollerFailed(Exception exception, Guid botInstanceId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Неподдерживаемая платформа поллера. BotInstanceId: {BotInstanceId}, Platform: {Platform}.")]
    private partial void LogUnsupportedPlatform(Guid botInstanceId, string platform);
}
