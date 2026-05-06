namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.DomainEventHandlers;

using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

internal sealed partial class ScenarioVersionDeletedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<ScenarioVersionDeletedDomainEventHandler> logger) : IDomainEventHandler<ScenarioVersionDeletedDomainEvent>
{
    public async Task HandleAsync(ScenarioVersionDeletedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new ScenarioVersionDeletedIntegrationEvent(@event.OccurredAt, @event.ProjectId, @event.DeletedVersion),
            cancellationToken);

        VersionDeleted(@event.ProjectId, @event.DeletedVersion);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано ScenarioVersionDeletedIntegrationEvent для проекта '{ProjectId}', версия {DeletedVersion}.")]
    private partial void VersionDeleted(Guid projectId, int deletedVersion);
    #endregion
}
