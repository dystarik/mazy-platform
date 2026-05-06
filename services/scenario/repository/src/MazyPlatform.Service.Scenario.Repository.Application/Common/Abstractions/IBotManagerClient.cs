namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

public interface IBotManagerClient
{
    Task<bool> HasBotsOnScenarioVersionAsync(Guid projectId, int scenarioVersion, CancellationToken cancellationToken = default);
}
