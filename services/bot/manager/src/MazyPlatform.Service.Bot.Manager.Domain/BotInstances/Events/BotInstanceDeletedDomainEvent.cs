namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

/// <summary>
/// Доменное событие, возникающее при удалении экземпляра бота.
/// </summary>
public sealed record BotInstanceDeletedDomainEvent : DomainEventBase
{
    internal BotInstanceDeletedDomainEvent(DateTimeOffset occurredAt, Guid botInstanceId, Guid? projectId)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
        ProjectId = projectId;
    }

    /// <summary>Идентификатор удалённого экземпляра бота.</summary>
    public Guid BotInstanceId { get; }

    /// <summary>Идентификатор проекта (если был привязан).</summary>
    public Guid? ProjectId { get; }
}
