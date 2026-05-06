namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

public sealed record ScenarioReleaseChangedDomainEvent : DomainEventBase
{
    internal ScenarioReleaseChangedDomainEvent(DateTimeOffset occurredAt, Guid scenarioGraphId, Guid projectId, int newVersion)
        : base(occurredAt)
    {
        ScenarioGraphId = scenarioGraphId;
        ProjectId = projectId;
        NewVersion = newVersion;
    }

    public Guid ScenarioGraphId { get; }

    public Guid ProjectId { get; }

    public int NewVersion { get; }
}
