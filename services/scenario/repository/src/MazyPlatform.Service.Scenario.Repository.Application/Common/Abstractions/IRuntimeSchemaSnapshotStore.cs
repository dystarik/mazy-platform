namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

public interface IRuntimeSchemaSnapshotStore
{
    Task UpsertProjectSchemasAsync(
        Guid projectId,
        int scenarioVersion,
        IReadOnlyCollection<EntitySchema> schemas,
        CancellationToken cancellationToken = default);
}
