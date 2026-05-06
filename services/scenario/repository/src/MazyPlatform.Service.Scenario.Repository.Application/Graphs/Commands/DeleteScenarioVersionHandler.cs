namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

internal sealed partial class DeleteScenarioVersionHandler(
    ILogger<DeleteScenarioVersionHandler> logger,
    IScenarioGraphRepository scenarioGraphRepository,
    IProjectRepository projectRepository,
    IBotManagerClient botManagerClient,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteScenarioVersionCommand>
{
    public async Task<Result> HandleAsync(DeleteScenarioVersionCommand command, CancellationToken cancellationToken = default)
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

        var hasBots = await botManagerClient.HasBotsOnScenarioVersionAsync(projectId, command.Version, cancellationToken);
        if (hasBots)
        {
            BotsExistOnVersion(projectId, command.Version);
            return Error.Conflict(
                ErrorCodes.ScenarioGraph.VersionInUse,
                $"На версии '{command.Version}' есть привязанные боты. Сначала переключите или удалите их.");
        }

        var result = graph.DeleteVersion(command.Version, timeProvider.GetUtcNow());
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        VersionDeleted(projectId, command.Version);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);

    [LoggerMessage(2, LogLevel.Warning, "На версии '{Version}' проекта '{ProjectId}' есть привязанные боты, удаление отклонено.")]
    private partial void BotsExistOnVersion(Guid projectId, int version);

    [LoggerMessage(3, LogLevel.Information, "Версия '{Version}' проекта '{ProjectId}' удалена.")]
    private partial void VersionDeleted(Guid projectId, int version);
    #endregion
}
