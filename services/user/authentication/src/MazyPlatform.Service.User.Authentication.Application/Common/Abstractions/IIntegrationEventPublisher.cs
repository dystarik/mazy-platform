namespace MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интерфейс публикации интеграционных событий.
/// </summary>
/// <remarks>
/// Используется прикладным слоем для отправки событий во внешнюю инфраструктуру обмена сообщениями.
/// </remarks>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Асинхронно публикует интеграционное событие.
    /// </summary>
    /// <param name="event">Интеграционное событие для публикации.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию публикации события.</returns>
    Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}
