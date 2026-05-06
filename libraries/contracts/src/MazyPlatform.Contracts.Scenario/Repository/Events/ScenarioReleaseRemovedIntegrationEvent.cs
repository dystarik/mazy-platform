namespace MazyPlatform.Contracts.Scenario.Repository.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Входящее интеграционное событие от <c>scenario-repository</c>, публикуемое при удалении всех версий релиза сценария.
/// </summary>
/// <remarks>
/// В ответ на это событие <c>scenario-bot-manager</c> деактивирует все активные боты проекта.
/// Уже неактивные боты остаются без изменений.
/// </remarks>
[IntegrationEventType("scenario.repository.scenario-release-removed")]
public sealed record ScenarioReleaseRemovedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ScenarioReleaseRemovedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="projectId">Идентификатор проекта, у которого удалены все версии релиза.</param>
    public ScenarioReleaseRemovedIntegrationEvent(DateTimeOffset occurredAt, Guid projectId)
        : base(occurredAt) => ProjectId = projectId;

    /// <summary>Идентификатор проекта, у которого удалены все версии релиза.</summary>
    public Guid ProjectId { get; }
}
