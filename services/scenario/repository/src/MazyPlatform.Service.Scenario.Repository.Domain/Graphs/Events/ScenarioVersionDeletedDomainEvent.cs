namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

public sealed record ScenarioVersionDeletedDomainEvent : DomainEventBase
{
    internal ScenarioVersionDeletedDomainEvent(DateTimeOffset occurredAt, Guid scenarioGraphId, Guid projectId, int deletedVersion)
        : base(occurredAt)
    {
        ScenarioGraphId = scenarioGraphId;
        ProjectId = projectId;
        DeletedVersion = deletedVersion;
    }

    public Guid ScenarioGraphId { get; }

    public Guid ProjectId { get; }

    public int DeletedVersion { get; }
}
