namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed class GetProjectListHandler(IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetProjectListQuery, GetProjectListResult>
{
    public async Task<Result<GetProjectListResult>> HandleAsync(GetProjectListQuery query, CancellationToken cancellationToken = default)
    {
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var items = await dbContext.Projects
            .Where(x => x.OwnerAccountId == ownerAccountId)
            .Select(p => new GetProjectListResult.ProjectListItem(p.Id, p.Name, p.PlatformType))
            .ToListAsync(cancellationToken);

        return new GetProjectListResult(items);
    }
}
