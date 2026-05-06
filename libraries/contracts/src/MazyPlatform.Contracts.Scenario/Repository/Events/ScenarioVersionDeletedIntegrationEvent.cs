namespace MazyPlatform.Contracts.Scenario.Repository.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Интеграционное событие от <c>scenario-repository</c>, публикуемое при удалении версии сценария.
/// </summary>
/// <remarks>
/// В ответ на это событие <c>scenario-bot-manager</c> может выполнить
/// очистку связанных данных для удалённой версии.
/// </remarks>
[IntegrationEventType("scenario.repository.scenario-version-deleted")]
public sealed record ScenarioVersionDeletedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ScenarioVersionDeletedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="projectId">Идентификатор проекта, у которого удалена версия сценария.</param>
    /// <param name="deletedVersion">Номер удалённой версии сценария.</param>
    public ScenarioVersionDeletedIntegrationEvent(DateTimeOffset occurredAt, Guid projectId, int deletedVersion)
        : base(occurredAt)
    {
        ProjectId = projectId;
        DeletedVersion = deletedVersion;
    }

    /// <summary>Идентификатор проекта, у которого удалена версия сценария.</summary>
    public Guid ProjectId { get; }

    /// <summary>Номер удалённой версии сценария.</summary>
    public int DeletedVersion { get; }
}
