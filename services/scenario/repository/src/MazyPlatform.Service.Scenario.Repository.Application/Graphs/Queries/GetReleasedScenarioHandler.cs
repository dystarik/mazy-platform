namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetReleasedScenarioHandler(
    ILogger<GetReleasedScenarioHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetReleasedScenarioQuery, GetReleasedScenarioResult>
{
    public async Task<Result<GetReleasedScenarioResult>> HandleAsync(GetReleasedScenarioQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);

        var graph = await dbContext.ScenarioGraphs
            .SingleOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);

        if (graph is null)
        {
            ReleasedScenarioNotFound(projectId);
            return Error.NotFound(ErrorCodes.ScenarioGraph.NotFound, $"Граф сценария для проекта '{projectId}' не найден.");
        }

        if (graph.CurrentReleaseVersion is null)
        {
            ReleasedScenarioNotFound(projectId);
            return Error.NotFound(ErrorCodes.ScenarioGraph.NoReleasedVersion, $"У проекта '{projectId}' нет опубликованного релиза.");
        }

        var version = graph.Versions.SingleOrDefault(v => v.Version == graph.CurrentReleaseVersion.Value);
        if (version is null)
        {
            InconsistentState(graph.Id, graph.CurrentReleaseVersion.Value);
            return Error.NotFound(ErrorCodes.ScenarioGraph.NoReleasedVersion, $"У проекта '{projectId}' нет опубликованного релиза.");
        }

        return new GetReleasedScenarioResult(graph.Id, version.GraphJson, version.Version);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Опубликованный граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ReleasedScenarioNotFound(Guid projectId);

    [LoggerMessage(2, LogLevel.Error, "Граф сценария '{ScenarioGraphId}' указывает на версию {Version}, которой нет в коллекции версий.")]
    private partial void InconsistentState(Guid scenarioGraphId, int version);
    #endregion
}
