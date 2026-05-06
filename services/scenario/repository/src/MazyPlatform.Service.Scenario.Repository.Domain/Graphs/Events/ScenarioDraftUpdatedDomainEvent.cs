namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;

public sealed record ScenarioDraftUpdatedDomainEvent : DomainEventBase
{
    internal ScenarioDraftUpdatedDomainEvent(DateTimeOffset occurredAt, Guid scenarioGraphId, Guid projectId)
        : base(occurredAt)
    {
        ScenarioGraphId = scenarioGraphId;
        ProjectId = projectId;
    }

    public Guid ScenarioGraphId { get; }

    public Guid ProjectId { get; }
}
