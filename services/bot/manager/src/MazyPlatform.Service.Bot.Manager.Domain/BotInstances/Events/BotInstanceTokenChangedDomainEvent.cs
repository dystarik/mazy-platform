namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

/// <summary>
/// Доменное событие, возникающее при смене токена доступа экземпляра бота.
/// </summary>
public sealed record BotInstanceTokenChangedDomainEvent : DomainEventBase
{
    internal BotInstanceTokenChangedDomainEvent(DateTimeOffset occurredAt, Guid botInstanceId)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
    }

    /// <summary>Идентификатор экземпляра бота.</summary>
    public Guid BotInstanceId { get; }
}
