namespace MazyPlatform.Contracts.Scenario.Repository.Events;

using MazyPlatform.Contracts.Core;

/// <summary>
/// Входящее интеграционное событие от <c>scenario-repository</c>, публикуемое при изменении текущей версии релиза сценария.
/// </summary>
/// <remarks>
/// В ответ на это событие <c>scenario-bot-manager</c> переключает все боты проекта на указанную версию сценария.
/// </remarks>
[IntegrationEventType("scenario.repository.scenario-release-changed")]
public sealed record ScenarioReleaseChangedIntegrationEvent : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ScenarioReleaseChangedIntegrationEvent"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="projectId">Идентификатор проекта, у которого изменилась версия релиза.</param>
    /// <param name="currentVersion">Новая текущая версия релиза сценария.</param>
    public ScenarioReleaseChangedIntegrationEvent(DateTimeOffset occurredAt, Guid projectId, int currentVersion)
        : base(occurredAt)
    {
        ProjectId = projectId;
        CurrentVersion = currentVersion;
    }

    /// <summary>Идентификатор проекта, у которого изменилась версия релиза.</summary>
    public Guid ProjectId { get; }

    /// <summary>Новая текущая версия релиза сценария.</summary>
    public int CurrentVersion { get; }
}
