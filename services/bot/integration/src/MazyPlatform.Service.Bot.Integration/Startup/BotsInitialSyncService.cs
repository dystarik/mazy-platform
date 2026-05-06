namespace MazyPlatform.Service.Bot.Integration.Startup;

using MazyPlatform.Service.Bot.Integration.Caching;
using MazyPlatform.Service.Bot.Integration.Grpc;
using MazyPlatform.Service.Bot.Integration.LongPoll;

internal sealed partial class BotsInitialSyncService(
    IServiceScopeFactory scopeFactory,
    BotInstanceCache cache,
    BotPollerOrchestrator orchestrator,
    ILogger<BotsInitialSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarting();

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IBotManagerClient>();

            var count = 0;

            await foreach (var entry in client.StreamActiveBotsAsync(stoppingToken))
            {
                cache.Set(entry.BotInstanceId, entry);
                orchestrator.StartPoller(entry);
                count++;
            }

            LogCompleted(count);
        }
        catch (Exception ex)
        {
            LogFailed(ex);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Начальная синхронизация активных ботов.")]
    private partial void LogStarting();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Синхронизация завершена. Запущено поллеров: {Count}.")]
    private partial void LogCompleted(int count);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Ошибка начальной синхронизации.")]
    private partial void LogFailed(Exception exception);
}
