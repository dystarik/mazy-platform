namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Extensions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

internal sealed partial class ValidateScenarioDraftHandler(
    ILogger<ValidateScenarioDraftHandler> logger,
    IPlatformScenarioValidator scenarioValidator,
    IProjectRepository projectRepository,
    IScenarioGraphRepository scenarioGraphRepository) : IQueryHandler<ValidateScenarioDraftQuery, ValidateScenarioDraftResult>
{
    public async Task<Result<ValidateScenarioDraftResult>> HandleAsync(ValidateScenarioDraftQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

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

        ScenarioValidationResult validationResult;

        try
        {
            validationResult = scenarioValidator.Validate(graph.DraftJson, project.PlatformType.ToScenarioPlatformKey());
        }
        catch (Exception ex)
        {
            ScenarioValidationFailedWithException(graph.Id, ex);
            return Error.Internal(
                ErrorCodes.ScenarioGraph.ValidationFailed,
                "Не удалось выполнить валидацию черновика сценария.");
        }

        return new ValidateScenarioDraftResult(validationResult.IsValid, validationResult.Errors);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);

    [LoggerMessage(2, LogLevel.Error, "Валидация черновика сценария для графа '{ScenarioGraphId}' завершилась необработанной ошибкой.")]
    private partial void ScenarioValidationFailedWithException(Guid scenarioGraphId, Exception exception);
    #endregion
}
