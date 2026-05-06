namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.DomainEventHandlers;

using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

internal sealed partial class ScenarioReleaseRemovedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<ScenarioReleaseRemovedDomainEventHandler> logger) : IDomainEventHandler<ScenarioReleaseRemovedDomainEvent>
{
    public async Task HandleAsync(ScenarioReleaseRemovedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new ScenarioReleaseRemovedIntegrationEvent(@event.OccurredAt, @event.ProjectId),
            cancellationToken);

        ReleaseRemoved(@event.ProjectId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано ScenarioReleaseRemovedIntegrationEvent для проекта '{ProjectId}'.")]
    private partial void ReleaseRemoved(Guid projectId);
    #endregion
}
