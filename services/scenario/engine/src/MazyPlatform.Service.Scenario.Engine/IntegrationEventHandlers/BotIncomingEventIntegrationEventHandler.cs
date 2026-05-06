namespace MazyPlatform.Service.Scenario.Engine.IntegrationEventHandlers;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Contracts.Bot.Manager;
using MazyPlatform.Contracts.Core;
using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Abstractions.Sessions;
using MazyPlatform.Scenario.Telegram.Events;
using MazyPlatform.Scenario.Vk.Events;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Platforms;
using MazyPlatform.Service.Scenario.Engine.Scenarios;

internal sealed partial class BotIncomingEventIntegrationEventHandler(
    IScenarioExecutor executor,
    ISessionStore sessionStore,
    BotInstanceCache botInstanceCache,
    ScenarioLoader scenarioLoader,
    ILogger<BotIncomingEventIntegrationEventHandler> logger)
    : IIntegrationEventHandler<BotIncomingEventIntegrationEvent>
{
    public async Task HandleAsync(BotIncomingEventIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        LogHandling(@event.BotId, @event.Platform.ToString());

        if (!ScenarioPlatformMapper.IsSupported(@event.Platform))
        {
            LogUnsupportedPlatform(@event.Platform.ToString());
            return;
        }

        if (!botInstanceCache.TryGet(@event.BotId, out var botEntry) || botEntry is null)
        {
            LogBotNotFound(@event.BotId);
            return;
        }

        var platformKey = ScenarioPlatformMapper.ToScenarioPlatformKey(@event.Platform);
        if (!string.Equals(botEntry.PlatformKey, platformKey, StringComparison.Ordinal))
        {
            LogPlatformMismatch(@event.BotId, botEntry.PlatformKey, platformKey);
            return;
        }

        var projectId = botEntry.ProjectId;
        var scenarioVersion = botEntry.ScenarioVersion;

        var graph = await scenarioLoader.GetOrLoadAsync(projectId, scenarioVersion, platformKey, cancellationToken);
        if (graph is null)
        {
            LogScenarioNotFound(@event.BotId, projectId, scenarioVersion);
            return;
        }

        var incomingEvent = ParseIncomingEvent(@event);
        var session = await sessionStore.GetOrCreateAsync(
            @event.BotId,
            incomingEvent.PlatformUserId,
            graph.StartNodeId,
            cancellationToken);

        if (IsStartCommand(@event.Platform, incomingEvent.Text))
        {
            await sessionStore.DeleteAsync(session.SessionId, cancellationToken);
            session = await sessionStore.GetOrCreateAsync(
                @event.BotId,
                incomingEvent.PlatformUserId,
                graph.StartNodeId,
                cancellationToken);

            LogSessionRestarted(@event.BotId, incomingEvent.PlatformUserId, session.SessionId);
        }

        var result = await executor.ExecuteAsync(
            graph,
            incomingEvent,
            session,
            projectId,
            scenarioVersion,
            botEntry.AccessToken,
            cancellationToken);

        await sessionStore.SaveAsync(session, cancellationToken);

        LogExecuted(@event.BotId, result.State.ToString(), result.Actions.Count);
    }

    private static IIncomingEvent ParseIncomingEvent(BotIncomingEventIntegrationEvent @event)
    {
        if (@event.Platform == PlatformType.Vk)
        {
            return VkEventParser.Parse(@event.RawPayload, @event.BotId);
        }

        if (ScenarioPlatformMapper.IsTelegram(@event.Platform))
        {
            return TelegramEventParser.Parse(@event.RawPayload, @event.BotId);
        }

        throw new InvalidOperationException($"Неподдерживаемая платформа: {@event.Platform}.");
    }

    private static bool IsStartCommand(PlatformType platform, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalizedText = text.Trim();
        if (ScenarioPlatformMapper.IsTelegram(platform))
            return IsTelegramStartCommand(normalizedText);

        return platform == PlatformType.Vk
            && string.Equals(normalizedText, "начать", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTelegramStartCommand(string text)
    {
        const string command = "/start";

        if (!text.StartsWith(command, StringComparison.OrdinalIgnoreCase))
            return false;

        return text.Length == command.Length
            || char.IsWhiteSpace(text[command.Length])
            || text[command.Length] == '@';
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Обработка входящего события. BotId: {BotId}, Platform: {Platform}.")]
    private partial void LogHandling(Guid botId, string platform);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Неподдерживаемая платформа: {Platform}.")]
    private partial void LogUnsupportedPlatform(string platform);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Бот не найден в кэше. BotId: {BotId}.")]
    private partial void LogBotNotFound(Guid botId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Сценарий не найден. BotId: {BotId}, ProjectId: {ProjectId}, Version: {Version}.")]
    private partial void LogScenarioNotFound(Guid botId, Guid projectId, int version);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Сценарий выполнен. BotId: {BotId}, State: {State}, Actions: {ActionsCount}.")]
    private partial void LogExecuted(Guid botId, string state, int actionsCount);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Платформа события не совпадает с кэшем бота. BotId: {BotId}, Cached: {CachedPlatform}, Event: {EventPlatform}.")]
    private partial void LogPlatformMismatch(Guid botId, string cachedPlatform, string eventPlatform);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Сессия перезапущена стартовой командой. BotId: {BotId}, PlatformUserId: {PlatformUserId}, NewSessionId: {SessionId}.")]
    private partial void LogSessionRestarted(Guid botId, string platformUserId, Guid sessionId);
}
