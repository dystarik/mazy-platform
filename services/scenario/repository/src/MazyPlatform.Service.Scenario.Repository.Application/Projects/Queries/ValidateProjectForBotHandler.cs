namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

internal sealed partial class ValidateProjectForBotHandler(
    ILogger<ValidateProjectForBotHandler> logger,
    IProjectRepository projectRepository,
    IScenarioGraphRepository scenarioGraphRepository) : ICommandHandler<ValidateProjectForBotQuery>
{
    public async Task<Result> HandleAsync(ValidateProjectForBotQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
            return Error.NotFound(ErrorCodes.Project.NotFound, $"Проект '{projectId}' не найден.");

        if (project.OwnerAccountId != ownerAccountId)
            return Error.Unauthorized(ErrorCodes.Project.AccessDenied, $"Нет доступа к проекту '{projectId}'.");

        if (project.PlatformType != PlatformType.Universal && project.PlatformType != query.PlatformType)
        {
            PlatformMismatch(projectId, project.PlatformType, query.PlatformType);
            return Error.Conflict(
                ErrorCodes.Project.PlatformMismatch,
                $"Тип платформы проекта '{projectId}' не совпадает с платформой бота.");
        }

        var graph = await scenarioGraphRepository.GetByProjectIdAsync(projectId, cancellationToken);
        if (graph is null)
        {
            return Error.NotFound(
                ErrorCodes.ScenarioGraph.NotFound,
                $"Граф сценария для проекта '{projectId}' не найден.");
        }

        var versionExists = graph.Versions.Any(v => v.Version == query.ScenarioVersion);
        if (!versionExists)
        {
            return Error.NotFound(
                ErrorCodes.ScenarioGraph.VersionNotFound,
                $"Версия '{query.ScenarioVersion}' сценария проекта '{projectId}' не найдена.");
        }

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning,
        "Тип платформы проекта '{ProjectId}' ({ProjectPlatform}) не совпадает с платформой бота ({RequestedPlatform}).")]
    private partial void PlatformMismatch(
        Guid projectId,
        PlatformType projectPlatform,
        PlatformType requestedPlatform);
    #endregion
}
