namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs;

public sealed class ScenarioVersion : Entity
{
    private ScenarioVersion() { }

    public required Guid ScenarioGraphId { get; init; }

    public required string GraphJson { get; init; }

    public required int Version { get; init; }

    public static ScenarioVersion CreateSnapshot(Guid scenarioGraphId, string graphJson, int version, DateTimeOffset now)
    {
        return new ScenarioVersion
        {
            Id = Guid.NewGuid(),
            ScenarioGraphId = scenarioGraphId,
            GraphJson = graphJson,
            Version = version,
            CreatedAt = now,
        };
    }
}
