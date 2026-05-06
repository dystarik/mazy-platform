namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.DomainEventHandlers;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.Events;

internal sealed partial class ProjectCreatedDomainEventHandler(
    IScenarioGraphRepository scenarioGraphRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProjectCreatedDomainEventHandler> logger) : IDomainEventHandler<ProjectCreatedDomainEvent>
{
    public async Task HandleAsync(ProjectCreatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var graph = ScenarioGraph.Create(@event.ProjectId, timeProvider.GetUtcNow());

        scenarioGraphRepository.Add(graph);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        ScenarioGraphCreated(graph.Id, @event.ProjectId);
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Information, "Граф сценария '{ScenarioGraphId}' создан для проекта '{ProjectId}'.")]
    private partial void ScenarioGraphCreated(Guid scenarioGraphId, Guid projectId);

    #endregion
}
