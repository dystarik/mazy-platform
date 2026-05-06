namespace MazyPlatform.Service.Bot.Integration.Messaging;

using MazyPlatform.Contracts.Bot.Integration.Events;

/// <summary>
/// Публикует интеграционные события в брокер сообщений.
/// </summary>
internal interface IEventPublisher
{
    /// <summary>
    /// Публикует событие входящего сообщения от бота.
    /// </summary>
    /// <param name="event">Событие.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task PublishAsync(BotIncomingEventIntegrationEvent @event, CancellationToken cancellationToken = default);
}
