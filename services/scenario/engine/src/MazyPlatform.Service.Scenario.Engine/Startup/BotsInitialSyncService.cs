namespace MazyPlatform.Service.Scenario.Engine.Startup;

using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Grpc;

internal sealed partial class BotsInitialSyncService(
    IServiceScopeFactory scopeFactory,
    BotInstanceCache cache,
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
        Message = "Начальная синхронизация списка активных ботов.")]
    private partial void LogStarting();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Синхронизация завершена. Загружено ботов: {Count}.")]
    private partial void LogCompleted(int count);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "Ошибка начальной синхронизации ботов.")]
    private partial void LogFailed(Exception exception);
}
