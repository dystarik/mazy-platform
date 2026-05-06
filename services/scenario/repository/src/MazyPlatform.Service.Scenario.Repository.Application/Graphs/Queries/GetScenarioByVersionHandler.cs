namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetScenarioByVersionHandler(
    ILogger<GetScenarioByVersionHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetScenarioByVersionQuery, GetScenarioByVersionResult>
{
    public async Task<Result<GetScenarioByVersionResult>> HandleAsync(GetScenarioByVersionQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);

        var graph = await dbContext.ScenarioGraphs
            .SingleOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);

        if (graph is null)
        {
            ScenarioGraphNotFound(projectId);
            return Error.NotFound(ErrorCodes.ScenarioGraph.NotFound, $"Граф сценария для проекта '{projectId}' не найден.");
        }

        var version = graph.Versions.SingleOrDefault(v => v.Version == query.Version);
        if (version is null)
        {
            VersionNotFound(projectId, query.Version);
            return Error.NotFound(ErrorCodes.ScenarioGraph.VersionNotFound, $"Версия '{query.Version}' не найдена.");
        }

        return new GetScenarioByVersionResult(graph.Id, version.GraphJson, version.Version);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);

    [LoggerMessage(2, LogLevel.Warning, "Версия '{Version}' графа сценария для проекта '{ProjectId}' не найдена.")]
    private partial void VersionNotFound(Guid projectId, int version);
    #endregion
}
