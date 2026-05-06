namespace MazyPlatform.Service.Scenario.Engine.Grpc;

internal interface IScenarioRepositoryClient
{
    Task<string?> GetScenarioJsonAsync(Guid projectId, int version, CancellationToken cancellationToken = default);
}
