namespace MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

public interface IEntitySchemaRepository : IRepository<EntitySchema>
{
    Task<IReadOnlyList<EntitySchema>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
