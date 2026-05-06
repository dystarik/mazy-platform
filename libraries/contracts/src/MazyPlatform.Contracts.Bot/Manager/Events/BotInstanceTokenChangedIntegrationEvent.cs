namespace MazyPlatform.Contracts.Bot.Manager.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие, публикуемое при изменении токена доступа экземпляра бота.
/// </summary>
/// <remarks>
/// Не содержит нового токена по соображениям безопасности.
/// Получатели должны запрашивать актуальные credentials через internal API при необходимости.
/// </remarks>
[IntegrationEventType("bot.manager.bot-instance-token-changed")]
public sealed record BotInstanceTokenChangedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BotInstanceTokenChangedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="botInstanceId">Идентификатор экземпляра бота, у которого изменился токен.</param>
    public BotInstanceTokenChangedIntegrationEvent(DateTimeOffset occurredAt, Guid botInstanceId)
        : base(occurredAt) => BotInstanceId = botInstanceId;

    /// <summary>Идентификатор экземпляра бота, у которого изменился токен.</summary>
    public Guid BotInstanceId { get; }
}
