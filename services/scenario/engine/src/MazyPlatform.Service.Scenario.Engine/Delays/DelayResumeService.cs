namespace MazyPlatform.Service.Scenario.Engine.Delays;

using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Abstractions.Sessions;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Configuration.Options;
using MazyPlatform.Service.Scenario.Engine.Scenarios;

using Microsoft.Extensions.Options;

internal sealed partial class DelayResumeService(
    IServiceScopeFactory scopeFactory,
    DelaySessionClaimStore claimStore,
    IOptions<DelayResumeOptions> options,
    ILogger<DelayResumeService> logger) : BackgroundService
{
    private const string _chatIdKey = "_chatId";

    private readonly DelayResumeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            LogDisabled();
            return;
        }

        LogStarted(_options.PollIntervalSeconds, _options.BatchSize);
        try
        {
            await claimStore.EnsureIndexesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            LogIndexInitializationFailed(ex);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ResumeDueSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogIterationFailed(ex);
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    private static string GetChatId(ISession session)
    {
        return session.Variables.TryGetValue(_chatIdKey, out var value)
            ? value?.ToString() ?? string.Empty
            : session.PlatformUserId;
    }

    private async Task ResumeDueSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = await claimStore.ClaimDueAsync(
            DateTimeOffset.UtcNow,
            _options.BatchSize,
            TimeSpan.FromSeconds(_options.LockSeconds),
            cancellationToken);

        if (sessions.Count == 0)
            return;

        LogClaimed(sessions.Count);

        foreach (var session in sessions)
        {
            await ResumeSessionAsync(session, cancellationToken);
        }
    }

    private async Task ResumeSessionAsync(ISession session, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var botInstanceCache = scope.ServiceProvider.GetRequiredService<BotInstanceCache>();
        var scenarioLoader = scope.ServiceProvider.GetRequiredService<ScenarioLoader>();
        var executor = scope.ServiceProvider.GetRequiredService<IScenarioExecutor>();
        var sessionStore = scope.ServiceProvider.GetRequiredService<ISessionStore>();

        if (!botInstanceCache.TryGet(session.BotId, out var botEntry) || botEntry is null)
        {
            LogBotNotFound(session.SessionId, session.BotId);
            return;
        }

        var graph = await scenarioLoader.GetOrLoadAsync(
            botEntry.ProjectId,
            botEntry.ScenarioVersion,
            botEntry.PlatformKey,
            cancellationToken);
        if (graph is null)
        {
            LogScenarioNotFound(session.SessionId, botEntry.ProjectId, botEntry.ScenarioVersion);
            return;
        }

        var incomingEvent = new DelayResumeIncomingEvent
        {
            BotId = session.BotId,
            PlatformUserId = session.PlatformUserId,
            ChatId = GetChatId(session),
        };
        var result = await executor.ExecuteAsync(
            graph,
            incomingEvent,
            session,
            botEntry.ProjectId,
            botEntry.ScenarioVersion,
            botEntry.AccessToken,
            cancellationToken);

        await sessionStore.SaveAsync(session, cancellationToken);

        LogResumed(session.SessionId, session.BotId, result.State.ToString(), result.Actions.Count);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Delay resume service запущен. PollIntervalSeconds: {PollIntervalSeconds}, BatchSize: {BatchSize}.")]
    private partial void LogStarted(int pollIntervalSeconds, int batchSize);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Delay resume service отключен конфигурацией.")]
    private partial void LogDisabled();

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug,
        Message = "Найдено delay-сессий для возобновления: {Count}.")]
    private partial void LogClaimed(int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "Бот для delay-сессии не найден в кэше. SessionId: {SessionId}, BotId: {BotId}.")]
    private partial void LogBotNotFound(Guid sessionId, Guid botId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Сценарий для delay-сессии не найден. SessionId: {SessionId}, ProjectId: {ProjectId}, Version: {Version}.")]
    private partial void LogScenarioNotFound(Guid sessionId, Guid projectId, int version);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information,
        Message = "Delay-сессия возобновлена. SessionId: {SessionId}, BotId: {BotId}, State: {State}, Actions: {ActionsCount}.")]
    private partial void LogResumed(Guid sessionId, Guid botId, string state, int actionsCount);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error,
        Message = "Ошибка итерации delay resume service.")]
    private partial void LogIterationFailed(Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning,
        Message = "Не удалось инициализировать MongoDB-индекс delay resume. Сервис продолжит работу без индекса.")]
    private partial void LogIndexInitializationFailed(Exception exception);
}
