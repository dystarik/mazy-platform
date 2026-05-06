namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при создании нового экземпляра бота.
/// </summary>
[IntegrationEventType("bot.manager.bot-instance-created")]
public sealed record BotInstanceCreatedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceCreatedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор созданного экземпляра бота.</param>
    /// <param name="projectId">Идентификатор проекта, к которому привязан бот, или <see langword="null"/>.</param>
    /// <param name="ownerAccountId">Идентификатор аккаунта владельца бота.</param>
    /// <param name="platformType">Тип платформы мессенджера.</param>
    public BotInstanceCreatedIntegrationEvent(
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

    /// <summary>Идентификатор проекта, к которому привязан бот, или <see langword="null"/>, если бот не привязан.</summary>
    public Guid? ProjectId { get; }

    /// <summary>Идентификатор аккаунта владельца бота.</summary>
    public Guid OwnerAccountId { get; }

    /// <summary>Тип платформы мессенджера.</summary>
    public PlatformType PlatformType { get; }
}
