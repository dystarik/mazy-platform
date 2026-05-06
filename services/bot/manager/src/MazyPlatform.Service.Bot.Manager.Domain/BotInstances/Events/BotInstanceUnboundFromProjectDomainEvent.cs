namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

/// <summary>
/// Доменное событие, возникающее при отвязке экземпляра бота от проекта.
/// </summary>
public sealed record BotInstanceUnboundFromProjectDomainEvent : DomainEventBase
{
    internal BotInstanceUnboundFromProjectDomainEvent(
        DateTimeOffset occurredAt,
        Guid botInstanceId,
        Guid formerProjectId)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
        FormerProjectId = formerProjectId;
    }

    /// <summary>Идентификатор экземпляра бота.</summary>
    public Guid BotInstanceId { get; }

    /// <summary>Идентификатор проекта, от которого был отвязан бот.</summary>
    public Guid FormerProjectId { get; }
}
