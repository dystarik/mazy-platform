namespace MazyPlatform.Service.Scenario.Repository.Application.UserData.Queries;

public sealed record GetUserDataRecordsResult(
    IReadOnlyList<GetUserDataRecordsResult.UserDataRecordItem> Items,
    int TotalCount)
{
    public sealed record UserDataRecordItem(
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
}
