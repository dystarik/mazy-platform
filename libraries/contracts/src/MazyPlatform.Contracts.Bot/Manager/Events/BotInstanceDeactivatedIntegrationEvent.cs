namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при деактивации экземпляра бота.
/// </summary>
[IntegrationEventType("bot.manager.bot-instance-deactivated")]
public sealed record BotInstanceDeactivatedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceDeactivatedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор деактивированного экземпляра бота.</param>
    public BotInstanceDeactivatedIntegrationEvent(DateTimeOffset occurredAt, Guid botInstanceId)
        : base(occurredAt) => BotInstanceId = botInstanceId;

    /// <summary>Идентификатор деактивированного экземпляра бота.</summary>
    public Guid BotInstanceId { get; }
}
