namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IEntitySchemaRepository"/>.
/// </summary>
internal sealed class EntitySchemaRepository(ApplicationDbContext context) : RepositoryBase<EntitySchema>(context), IEntitySchemaRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntitySchema>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.EntitySchemas
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }
}
