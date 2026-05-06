namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

/// <summary>
/// Доменное событие, возникающее при активации экземпляра бота.
/// </summary>
public sealed record BotInstanceActivatedDomainEvent : DomainEventBase
{
    internal BotInstanceActivatedDomainEvent(
        DateTimeOffset occurredAt,
        Guid botInstanceId,
        Guid projectId,
        PlatformType platformType,
        string? communityId,
        int scenarioVersion)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
        ProjectId = projectId;
        PlatformType = platformType;
        CommunityId = communityId;
        ScenarioVersion = scenarioVersion;
    }

    /// <summary>Идентификатор активированного экземпляра бота.</summary>
    public Guid BotInstanceId { get; }

    /// <summary>Идентификатор проекта.</summary>
    public Guid ProjectId { get; }

    /// <summary>Тип платформы.</summary>
    public PlatformType PlatformType { get; }

    /// <summary>Идентификатор сообщества (для VK).</summary>
    public string? CommunityId { get; }

    /// <summary>Версия привязанного сценария.</summary>
    public int ScenarioVersion { get; }
}
