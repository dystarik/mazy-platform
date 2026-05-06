namespace MazyPlatform.Service.Scenario.Repository.Domain.Projects;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects.Events;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

public sealed class Project : AggregateRoot
{
    private Project() { }

    public required Guid OwnerAccountId { get; init; }

    public string Name { get; private set; } = null!;

    public required PlatformType PlatformType { get; init; }

    public static Project Create(Guid ownerAccountId, string name, PlatformType platformType, DateTimeOffset now)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = ownerAccountId,
            Name = name,
            PlatformType = platformType,
            CreatedAt = now,
        };
        project.AddDomainEvent(new ProjectCreatedDomainEvent(now, project.Id, ownerAccountId));
        return project;
    }

    public void Delete(DateTimeOffset now)
    {
        MarkAsUpdated(now);
        AddDomainEvent(new ProjectDeletedDomainEvent(now, Id, OwnerAccountId));
    }
}
