namespace MazyPlatform.Contracts.Core;

/// <summary>
/// Обработчик интеграционного события типа <typeparamref name="TEvent"/>.
/// Реализации содержат логику обработки конкретного события полученного из брокера сообщений.
/// </summary>
/// <typeparam name="TEvent">Тип интеграционного события, реализующий <see cref="IIntegrationEvent"/>.</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Асинхронно обрабатывает интеграционное событие.
    /// </summary>
    /// <param name="event">Событие которое необходимо обработать.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see cref="Task"/>, представляющая асинхронную операцию обработки.</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
