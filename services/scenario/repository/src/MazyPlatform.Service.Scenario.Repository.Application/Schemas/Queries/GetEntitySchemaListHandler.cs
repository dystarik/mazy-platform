namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed class GetEntitySchemaListHandler(
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetEntitySchemaListQuery, GetEntitySchemaListResult>
{
    public async Task<Result<GetEntitySchemaListResult>> HandleAsync(GetEntitySchemaListQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var items = await dbContext.EntitySchemas
            .Where(x => x.ProjectId == projectId)
            .Select(s => new GetEntitySchemaListResult.EntitySchemaListItem(s.Id, s.Name))
            .ToListAsync(cancellationToken);

        return new GetEntitySchemaListResult(items);
    }
}
