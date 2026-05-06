namespace MazyPlatform.Service.Bot.Manager.Domain.Shared;

using MazyPlatform.SharedKernel.Domain.Abstractions;

/// <summary>
/// Базовый тип для всех доменных событий сервиса.
/// </summary>
public abstract record DomainEventBase(DateTimeOffset OccurredAt) : IDomainEvent
{
    /// <summary>Уникальный идентификатор события.</summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
