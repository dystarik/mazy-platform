namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IProjectRepository"/>.
/// </summary>
internal sealed class ProjectRepository(ApplicationDbContext context) : RepositoryBase<Project>(context), IProjectRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetByOwnerAccountIdAsync(Guid ownerAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(x => x.OwnerAccountId == ownerAccountId)
            .ToListAsync(cancellationToken);
    }
}
