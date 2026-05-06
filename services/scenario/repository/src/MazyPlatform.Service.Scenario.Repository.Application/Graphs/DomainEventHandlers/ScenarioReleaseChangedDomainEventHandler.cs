namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.DomainEventHandlers;

using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

internal sealed partial class ScenarioReleaseChangedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<ScenarioReleaseChangedDomainEventHandler> logger) : IDomainEventHandler<ScenarioReleaseChangedDomainEvent>
{
    public async Task HandleAsync(ScenarioReleaseChangedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new ScenarioReleaseChangedIntegrationEvent(@event.OccurredAt, @event.ProjectId, @event.NewVersion),
            cancellationToken);

        ReleaseChanged(@event.ProjectId, @event.NewVersion);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано ScenarioReleaseChangedIntegrationEvent для проекта '{ProjectId}', новая версия {NewVersion}.")]
    private partial void ReleaseChanged(Guid projectId, int newVersion);
    #endregion
}
