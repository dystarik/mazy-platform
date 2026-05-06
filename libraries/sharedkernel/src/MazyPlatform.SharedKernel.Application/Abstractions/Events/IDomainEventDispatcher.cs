namespace MazyPlatform.SharedKernel.Application.Abstractions.Events;

using MazyPlatform.SharedKernel.Domain.Abstractions;

/// <summary>
/// Диспетчер доменных событий: отвечает за распространение набора доменных событий
/// соответствующим обработчикам и за координацию их асинхронной обработки.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Отправляет коллекцию доменных событий на обработку.
    /// </summary>
    /// <param name="domainEvents">Коллекция доменных событий для обработки. Не должна быть <see langword="null"/> и может содержать ноль или более событий.</param>
    /// <param name="cancellationToken">Токен отмены для операции.</param>
    /// <returns>
    /// Задача, представляющая асинхронную операцию.
    /// </returns>
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
