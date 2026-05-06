namespace MazyPlatform.Service.Scenario.Repository.Domain.Projects;

public interface IProjectRepository : IRepository<Project>
{
    Task<IReadOnlyList<Project>> GetByOwnerAccountIdAsync(Guid ownerAccountId, CancellationToken cancellationToken = default);
}
