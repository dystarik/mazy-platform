namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при активации экземпляра бота.
/// </summary>
[IntegrationEventType("bot.manager.bot-instance-activated")]
public sealed record BotInstanceActivatedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceActivatedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор активированного экземпляра бота.</param>
    /// <param name="projectId">Идентификатор проекта, к которому привязан бот.</param>
    /// <param name="platformType">Тип платформы мессенджера.</param>
    /// <param name="communityId">Идентификатор сообщества (для ВКонтакте), или <see langword="null"/>.</param>
    /// <param name="scenarioVersion">Версия сценария, по которой работает бот.</param>
    public BotInstanceActivatedIntegrationEvent(
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

    /// <summary>Идентификатор проекта, к которому привязан бот.</summary>
    public Guid ProjectId { get; }

    /// <summary>Тип платформы мессенджера.</summary>
    public PlatformType PlatformType { get; }

    /// <summary>Идентификатор сообщества (для ВКонтакте), или <see langword="null"/> для других платформ.</summary>
    public string? CommunityId { get; }

    /// <summary>Версия сценария, по которой работает бот.</summary>
    public int ScenarioVersion { get; }
}
