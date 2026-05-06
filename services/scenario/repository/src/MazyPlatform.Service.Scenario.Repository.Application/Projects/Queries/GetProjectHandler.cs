namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetProjectHandler(
    ILogger<GetProjectHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetProjectQuery, GetProjectResult>
{
    public async Task<Result<GetProjectResult>> HandleAsync(GetProjectQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var project = await dbContext.Projects
            .SingleOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null)
        {
            ProjectNotFound(projectId);
            return Error.NotFound(ErrorCodes.Project.NotFound, $"Проект '{projectId}' не найден.");
        }

        if (project.OwnerAccountId != ownerAccountId)
        {
            return Error.Unauthorized(ErrorCodes.Project.AccessDenied, $"Нет доступа к проекту '{projectId}'.");
        }

        return new GetProjectResult(project.Id, project.Name, project.PlatformType);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Проект '{ProjectId}' не найден.")]
    private partial void ProjectNotFound(Guid projectId);
    #endregion
}
