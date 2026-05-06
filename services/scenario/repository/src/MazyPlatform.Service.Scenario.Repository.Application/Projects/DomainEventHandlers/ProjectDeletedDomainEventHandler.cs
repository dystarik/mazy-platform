namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.DomainEventHandlers;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.Events;

internal sealed partial class ProjectDeletedDomainEventHandler(
    IScenarioGraphRepository scenarioGraphRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProjectDeletedDomainEventHandler> logger) : IDomainEventHandler<ProjectDeletedDomainEvent>
{
    public async Task HandleAsync(ProjectDeletedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var graph = await scenarioGraphRepository.GetByProjectIdAsync(@event.ProjectId, cancellationToken);

        if (graph is null)
        {
            return;
        }

        scenarioGraphRepository.Delete(graph);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ScenarioGraphDeleted(graph.Id, @event.ProjectId);
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Information, "Граф сценария '{ScenarioGraphId}' удалён для проекта '{ProjectId}'.")]
    private partial void ScenarioGraphDeleted(Guid scenarioGraphId, Guid projectId);

    #endregion
}
