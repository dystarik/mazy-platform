namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed class HasBotsOnScenarioVersionHandler(IReadOnlyApplicationDbContext dbContext) : IQueryHandler<HasBotsOnScenarioVersionQuery, bool>
{
    public async Task<Result<bool>> HandleAsync(HasBotsOnScenarioVersionQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);

        return await dbContext.BotInstances
            .AnyAsync(x => x.ProjectId == projectId && x.ScenarioVersion == query.ScenarioVersion, cancellationToken);
    }
}
