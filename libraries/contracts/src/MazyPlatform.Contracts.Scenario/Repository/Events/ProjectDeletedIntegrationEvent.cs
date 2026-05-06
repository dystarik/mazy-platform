namespace MazyPlatform.Contracts.Scenario.Repository.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Входящее интеграционное событие от <c>scenario-repository</c>, публикуемое при удалении проекта.
/// </summary>
/// <remarks>
/// В ответ на это событие <c>scenario-bot-manager</c> отвязывает всех ботов, привязанных к удалённому проекту.
/// </remarks>
[IntegrationEventType("scenario.repository.project-deleted")]
public sealed record ProjectDeletedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ProjectDeletedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="projectId">Идентификатор удалённого проекта.</param>
    public ProjectDeletedIntegrationEvent(DateTimeOffset occurredAt, Guid projectId)
        : base(occurredAt) => ProjectId = projectId;

    /// <summary>Идентификатор удалённого проекта.</summary>
    public Guid ProjectId { get; }
}
