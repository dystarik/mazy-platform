namespace MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Базовый класс для всех доменных событий сервиса аутентификации.
/// </summary>
/// <param name="OccurredAt">Временная метка UTC, когда произошло событие.</param>
/// <remarks>
/// Каждый экземпляр получает уникальный <see cref="EventId"/> при создании.
/// Конкретные события наследуются от <see cref="UserAccountDomainEventBase"/>,
/// <see cref="MfaSessionDomainEventBase"/> или напрямую от этого класса.
/// </remarks>
public abstract record class DomainEventBase(DateTimeOffset OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Уникальный идентификатор экземпляра события, генерируется автоматически.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
