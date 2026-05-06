namespace MazyPlatform.Contracts.Core;

/// <summary>
/// Базовый класс для всех интеграционных событий платформы.
/// Содержит общие свойства которые должны быть у каждого интеграционного события.
/// </summary>
/// <remarks>
/// Все интеграционные события должны наследоваться от этого класса
/// и иметь атрибут <see cref="IntegrationEventTypeAttribute"/> с ключом маршрутизации.
/// </remarks>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IntegrationEventBase"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    protected IntegrationEventBase(DateTimeOffset occurredAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = occurredAt;
    }

    /// <inheritdoc/>
    public Guid EventId { get; }

    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; }
}
