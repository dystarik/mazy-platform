namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetVersionHistoryHandler(
    ILogger<GetVersionHistoryHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetVersionHistoryQuery, GetVersionHistoryResult>
{
    public async Task<Result<GetVersionHistoryResult>> HandleAsync(GetVersionHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var graph = await dbContext.ScenarioGraphs
            .SingleOrDefaultAsync(
                x => x.ProjectId == projectId
                    && dbContext.Projects.Any(project => project.Id == x.ProjectId && project.OwnerAccountId == ownerAccountId),
                cancellationToken);

        if (graph is null)
        {
            ScenarioGraphNotFound(projectId);
            return Error.NotFound(ErrorCodes.ScenarioGraph.NotFound, $"Граф сценария для проекта '{projectId}' не найден.");
        }

        var items = graph.Versions
            .OrderByDescending(v => v.Version)
            .Select(v => new GetVersionHistoryResult.VersionItem(
                v.Version,
                v.CreatedAt,
                IsCurrent: v.Version == graph.CurrentReleaseVersion))
            .ToList();

        return new GetVersionHistoryResult(items);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);
    #endregion
}
