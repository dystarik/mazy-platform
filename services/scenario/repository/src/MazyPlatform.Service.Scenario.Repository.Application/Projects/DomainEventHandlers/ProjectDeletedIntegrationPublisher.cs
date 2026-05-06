namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.DomainEventHandlers;

using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.Events;

internal sealed partial class ProjectDeletedIntegrationPublisher(
    IIntegrationEventPublisher publisher,
    ILogger<ProjectDeletedIntegrationPublisher> logger) : IDomainEventHandler<ProjectDeletedDomainEvent>
{
    public async Task HandleAsync(ProjectDeletedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(
            new ProjectDeletedIntegrationEvent(@event.OccurredAt, @event.ProjectId),
            cancellationToken);

        IntegrationEventPublished(@event.ProjectId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано ProjectDeletedIntegrationEvent для проекта '{ProjectId}'.")]
    private partial void IntegrationEventPublished(Guid projectId);
    #endregion
}
