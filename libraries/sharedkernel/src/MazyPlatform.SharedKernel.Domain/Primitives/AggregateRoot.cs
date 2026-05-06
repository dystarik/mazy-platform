namespace MazyPlatform.SharedKernel.Domain.Primitives;

using MazyPlatform.SharedKernel.Domain.Abstractions;

/// <summary>
/// Базовый класс для корней агрегатов в доменной модели.
/// Предоставляет поддержку накопления доменных событий и методы управления ними.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Коллекция доменных событий, накопленных агрегатом.
    /// </summary>
    /// <value>Коллекция <see cref="IDomainEvent"/>, доступная только для чтения.</value>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Возвращает коллекцию доменных событий и очищает её атомарно.
    /// Предназначен для инфраструктурного слоя (например, UnitOfWork) для диспетчеризации событий.
    /// </summary>
    /// <returns>Коллекция <see cref="IDomainEvent"/>, доступная только для чтения.</returns>
    public IReadOnlyCollection<IDomainEvent> GetAndClearDomainEvents()
    {
        var events = _domainEvents.ToArray().AsReadOnly();
        ClearDomainEvents();
        return events;
    }

    /// <summary>
    /// Очищает накопленные доменные события.
    /// </summary>
    protected void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Добавляет доменное событие в коллекцию.
    /// </summary>
    /// <param name="domainEvent">Событие, которое необходимо добавить.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
