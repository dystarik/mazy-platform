namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

internal sealed partial class UpdateScenarioDraftHandler(
    ILogger<UpdateScenarioDraftHandler> logger,
    IScenarioGraphRepository scenarioGraphRepository,
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateScenarioDraftCommand>
{
    public async Task<Result> HandleAsync(UpdateScenarioDraftCommand command, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(command.ProjectId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

        if (project is null)
            return Error.NotFound(ErrorCodes.Project.NotFound, $"Проект '{projectId}' не найден.");

        if (project.OwnerAccountId != ownerAccountId)
            return Error.Unauthorized(ErrorCodes.Project.AccessDenied, $"Нет доступа к проекту '{projectId}'.");

        var graph = await scenarioGraphRepository.GetByProjectIdAsync(projectId, cancellationToken);

        if (graph is null)
        {
            ScenarioGraphNotFound(projectId);
            return Error.NotFound(ErrorCodes.ScenarioGraph.NotFound, $"Граф сценария для проекта '{projectId}' не найден.");
        }

        graph.UpdateDraft(command.GraphJson, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);
    #endregion
}
