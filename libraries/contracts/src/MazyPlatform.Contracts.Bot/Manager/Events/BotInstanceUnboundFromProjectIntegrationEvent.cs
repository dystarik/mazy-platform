namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при отвязке экземпляра бота от проекта.
/// </summary>
/// <remarks>
/// Отвязка происходит автоматически при удалении проекта. Бот при этом не удаляется —
/// владелец может впоследствии привязать его к другому проекту.
/// </remarks>
[IntegrationEventType("bot.manager.bot-instance-unbound-from-project")]
public sealed record BotInstanceUnboundFromProjectIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceUnboundFromProjectIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор экземпляра бота.</param>
    /// <param name="formerProjectId">Идентификатор проекта, от которого был отвязан бот.</param>
    public BotInstanceUnboundFromProjectIntegrationEvent(
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
