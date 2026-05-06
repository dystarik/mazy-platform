namespace MazyPlatform.Contracts.Bot.Integration.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие входящего сообщения от пользователя бота.
/// Публикуется сервисом интеграции после получения события от платформы мессенджера.
/// </summary>
[IntegrationEventType("bot.integration.incoming_event")]
public sealed record BotIncomingEventIntegrationEvent : IntegrationEventBase
{
    /// <summary>Инициализирует новый экземпляр <see cref="BotIncomingEventIntegrationEvent"/>.</summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="platform">Тип платформы мессенджера.</param>
    /// <param name="botId">Идентификатор бота.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="scenarioVersion">Версия сценария.</param>
    /// <param name="rawPayload">Сырой JSON-payload события от платформы.</param>
    public BotIncomingEventIntegrationEvent(
        DateTimeOffset occurredAt,
        PlatformType platform,
        Guid botId,
        Guid projectId,
        int scenarioVersion,
        string rawPayload)
        : base(occurredAt)
    {
        Platform = platform;
        BotId = botId;
        ProjectId = projectId;
        ScenarioVersion = scenarioVersion;
        RawPayload = rawPayload;
    }

    /// <summary>Тип платформы мессенджера.</summary>
    public PlatformType Platform { get; }

    /// <summary>Идентификатор бота.</summary>
    public Guid BotId { get; }

    /// <summary>Идентификатор проекта.</summary>
    public Guid ProjectId { get; }

    /// <summary>Версия сценария.</summary>
    public int ScenarioVersion { get; }

    /// <summary>Сырой JSON-payload события от платформы.</summary>
    public string RawPayload { get; }
}
