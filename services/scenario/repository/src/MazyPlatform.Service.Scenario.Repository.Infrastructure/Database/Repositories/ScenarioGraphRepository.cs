namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IScenarioGraphRepository"/>.
/// </summary>
internal sealed class ScenarioGraphRepository(ApplicationDbContext context) : RepositoryBase<ScenarioGraph>(context), IScenarioGraphRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public async Task<ScenarioGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ScenarioGraphs
            .SingleOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
    }
}
