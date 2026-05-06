namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

/// <summary>
/// Доменное событие, возникающее при деактивации экземпляра бота.
/// </summary>
public sealed record BotInstanceDeactivatedDomainEvent : DomainEventBase
{
    internal BotInstanceDeactivatedDomainEvent(DateTimeOffset occurredAt, Guid botInstanceId)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
    }

    /// <summary>Идентификатор деактивированного экземпляра бота.</summary>
    public Guid BotInstanceId { get; }
}
