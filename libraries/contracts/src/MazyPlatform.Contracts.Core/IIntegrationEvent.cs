namespace MazyPlatform.Contracts.Core;

/// <summary>
/// Представляет интеграционное событие с уникальным идентификатором и временем возникновения.
/// Интеграционные события используются для асинхронного взаимодействия между сервисами через брокер сообщений.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    /// <value>Значение типа <see cref="Guid"/> — уникальный идентификатор события.</value>
    Guid EventId { get; }

    /// <summary>
    /// Временная метка возникновения события.
    /// </summary>
    /// <value>Значение типа <see cref="DateTimeOffset"/> — время возникновения события.</value>
    DateTimeOffset OccurredAt { get; }
}
