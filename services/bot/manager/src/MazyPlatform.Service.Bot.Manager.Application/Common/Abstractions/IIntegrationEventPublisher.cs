namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;

/// <summary>
/// Контракт публикации интеграционных событий в шину сообщений.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Публикует интеграционное событие.</summary>
    /// <typeparam name="TEvent">Тип события.</typeparam>
    /// <param name="integrationEvent">Экземпляр события.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию публикации.</returns>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
