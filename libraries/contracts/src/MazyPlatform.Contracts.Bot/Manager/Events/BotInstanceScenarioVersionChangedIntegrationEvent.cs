namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при изменении версии сценария экземпляра бота.
/// </summary>
[IntegrationEventType("bot.manager.bot-instance-scenario-version-changed")]
public sealed record BotInstanceScenarioVersionChangedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceScenarioVersionChangedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор экземпляра бота.</param>
    /// <param name="newScenarioVersion">Новая версия сценария.</param>
    public BotInstanceScenarioVersionChangedIntegrationEvent(
        DateTimeOffset occurredAt,
        Guid botInstanceId,
        int newScenarioVersion)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
        NewScenarioVersion = newScenarioVersion;
    }

    /// <summary>Идентификатор экземпляра бота.</summary>
    public Guid BotInstanceId { get; }

    /// <summary>Новая версия сценария, назначенная боту.</summary>
    public int NewScenarioVersion { get; }
}
