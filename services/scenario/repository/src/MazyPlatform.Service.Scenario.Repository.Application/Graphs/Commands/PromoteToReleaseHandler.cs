namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Extensions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

internal sealed partial class PromoteToReleaseHandler(
    ILogger<PromoteToReleaseHandler> logger,
    IPlatformScenarioValidator scenarioValidator,
    IScenarioGraphRepository scenarioGraphRepository,
    IProjectRepository projectRepository,
    IEntitySchemaRepository entitySchemaRepository,
    IRuntimeSchemaSnapshotStore runtimeSchemaSnapshotStore,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<PromoteToReleaseCommand>
{
    public async Task<Result> HandleAsync(PromoteToReleaseCommand command, CancellationToken cancellationToken = default)
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

        ScenarioValidationResult validationResult;

        try
        {
            validationResult = scenarioValidator.Validate(graph.DraftJson, project.PlatformType.ToScenarioPlatformKey());
        }
        catch (Exception ex)
        {
            ScenarioValidationFailedWithException(graph.Id, ex);
            return Error.Internal(
                ErrorCodes.ScenarioGraph.PromotionFailed,
                "Не удалось выполнить валидацию сценария перед публикацией.");
        }

        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.ToErrorCollection();
            PromotionFailed(graph.Id, validationErrors);
            return Result.Failure(validationErrors);
        }

        var result = graph.Promote([], timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            PromotionFailed(graph.Id, result.Errors);
            return result;
        }

        var scenarioVersion = graph.CurrentReleaseVersion!.Value;
        var schemas = await entitySchemaRepository.GetByProjectIdAsync(projectId, cancellationToken);

        try
        {
            await runtimeSchemaSnapshotStore.UpsertProjectSchemasAsync(
                projectId,
                scenarioVersion,
                schemas,
                cancellationToken);
        }
        catch (Exception ex)
        {
            RuntimeSchemaSnapshotFailed(projectId, scenarioVersion, ex);
            return Error.Internal(
                ErrorCodes.ScenarioGraph.PromotionFailed,
                "Не удалось сохранить snapshot схем данных для опубликованной версии сценария.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);

    [LoggerMessage(2, LogLevel.Warning, "Продвижение графа сценария '{ScenarioGraphId}' завершилось с ошибками: {Errors}.")]
    private partial void PromotionFailed(Guid scenarioGraphId, object errors);

    [LoggerMessage(3, LogLevel.Error, "Валидация сценария для графа '{ScenarioGraphId}' завершилась необработанной ошибкой.")]
    private partial void ScenarioValidationFailedWithException(Guid scenarioGraphId, Exception exception);

    [LoggerMessage(4, LogLevel.Error, "Не удалось сохранить runtime snapshot схем. ProjectId: {ProjectId}, Version: {Version}.")]
    private partial void RuntimeSchemaSnapshotFailed(Guid projectId, int version, Exception exception);
    #endregion
}
