namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetScenarioDraftHandler(
    ILogger<GetScenarioDraftHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetScenarioDraftQuery, GetScenarioDraftResult>
{
    public async Task<Result<GetScenarioDraftResult>> HandleAsync(GetScenarioDraftQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var graph = await dbContext.ScenarioGraphs
            .SingleOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);

        if (graph is not null)
            return new GetScenarioDraftResult(graph.Id, graph.DraftJson, Version: 0);

        ScenarioGraphNotFound(projectId);
        return Error.NotFound(ErrorCodes.ScenarioGraph.NotFound, $"Граф сценария для проекта '{projectId}' не найден.");
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Граф сценария для проекта '{ProjectId}' не найден.")]
    private partial void ScenarioGraphNotFound(Guid projectId);
    #endregion
}
