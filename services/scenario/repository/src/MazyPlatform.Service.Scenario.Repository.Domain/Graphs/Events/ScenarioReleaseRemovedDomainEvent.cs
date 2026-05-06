namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

public sealed record ScenarioReleaseRemovedDomainEvent : DomainEventBase
{
    internal ScenarioReleaseRemovedDomainEvent(DateTimeOffset occurredAt, Guid scenarioGraphId, Guid projectId)
        : base(occurredAt)
    {
        ScenarioGraphId = scenarioGraphId;
        ProjectId = projectId;
    }

    public Guid ScenarioGraphId { get; }

    public Guid ProjectId { get; }
}
