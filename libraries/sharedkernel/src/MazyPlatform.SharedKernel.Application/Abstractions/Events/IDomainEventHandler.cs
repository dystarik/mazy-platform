namespace MazyPlatform.SharedKernel.Application.Abstractions.Events;

using MazyPlatform.SharedKernel.Domain.Abstractions;

/// <summary>
/// Обработчик доменного события.
/// Обработчики получают доменные события и выполняют сопутствующую логику (проекции, интеграции, побочные эффекты и т.п.).
/// </summary>
/// <typeparam name="TEvent">Тип доменного события. Должен реализовывать <see cref="IDomainEvent"/>.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Обработать доменное событие асинхронно.
    /// </summary>
    /// <param name="domainEvent">Экземпляр доменного события для обработки. Не должен быть <see langword="null"/>.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Задача, представляющая асинхронную операцию.
    /// </returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
