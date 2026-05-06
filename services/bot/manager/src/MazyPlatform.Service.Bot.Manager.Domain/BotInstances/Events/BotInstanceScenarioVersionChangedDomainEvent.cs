namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

/// <summary>
/// Доменное событие, возникающее при изменении версии сценария экземпляра бота.
/// </summary>
public sealed record BotInstanceScenarioVersionChangedDomainEvent : DomainEventBase
{
    internal BotInstanceScenarioVersionChangedDomainEvent(
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

    /// <summary>Новая версия сценария.</summary>
    public int NewScenarioVersion { get; }
}
