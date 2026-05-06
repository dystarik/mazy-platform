namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

using Microsoft.EntityFrameworkCore;

internal sealed class BotInstanceRepository(ApplicationDbContext context) : RepositoryBase<BotInstance>(context), IBotInstanceRepository
{
    public Task<BotInstance?> GetByIdAndOwnerAsync(Guid id, Guid ownerAccountId, CancellationToken cancellationToken = default)
    {
        return Context.BotInstances
            .SingleOrDefaultAsync(x => x.Id == id && x.OwnerAccountId == ownerAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<BotInstance>> GetAllByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.BotInstances
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }
}
