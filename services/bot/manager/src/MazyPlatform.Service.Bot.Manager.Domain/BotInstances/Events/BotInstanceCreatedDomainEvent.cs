namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

/// <summary>
/// Доменное событие, возникающее при создании нового экземпляра бота.
/// </summary>
public sealed record BotInstanceCreatedDomainEvent : DomainEventBase
{
    internal BotInstanceCreatedDomainEvent(
        DateTimeOffset occurredAt,
        Guid botInstanceId,
        Guid? projectId,
        Guid ownerAccountId,
        PlatformType platformType)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
        ProjectId = projectId;
        OwnerAccountId = ownerAccountId;
        PlatformType = platformType;
    }

    /// <summary>Идентификатор созданного экземпляра бота.</summary>
    public Guid BotInstanceId { get; }

    /// <summary>Идентификатор проекта.</summary>
    public Guid? ProjectId { get; }

    /// <summary>Идентификатор владельца.</summary>
    public Guid OwnerAccountId { get; }

    /// <summary>Тип платформы.</summary>
    public PlatformType PlatformType { get; }
}
