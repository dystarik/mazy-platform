namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при удалении экземпляра бота.
/// </summary>
[IntegrationEventType("bot.manager.bot-instance-deleted")]
public sealed record BotInstanceDeletedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceDeletedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор удалённого экземпляра бота.</param>
    /// <param name="projectId">Идентификатор проекта, к которому был привязан бот, или <see langword="null"/>.</param>
    public BotInstanceDeletedIntegrationEvent(DateTimeOffset occurredAt, Guid botInstanceId, Guid? projectId)
        : base(occurredAt)
    {
        BotInstanceId = botInstanceId;
        ProjectId = projectId;
    }

    /// <summary>Идентификатор удалённого экземпляра бота.</summary>
    public Guid BotInstanceId { get; }

    /// <summary>Идентификатор проекта, к которому был привязан бот, или <see langword="null"/>, если бот не был привязан.</summary>
    public Guid? ProjectId { get; }
}
