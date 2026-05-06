namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

public sealed record RuntimeUserDataRecord(
    Guid RecordId,
    Guid SchemaId,
    Guid SchemaSnapshotId,
    string SchemaName,
    int ScenarioVersion,
    Guid BotId,
    string PlatformUserId,
    Guid? SessionId,
    bool IsArchived,
    string DataJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
