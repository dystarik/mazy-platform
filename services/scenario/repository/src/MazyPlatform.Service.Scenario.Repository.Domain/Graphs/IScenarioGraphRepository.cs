namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs;

public interface IScenarioGraphRepository : IRepository<ScenarioGraph>
{
    Task<ScenarioGraph?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
