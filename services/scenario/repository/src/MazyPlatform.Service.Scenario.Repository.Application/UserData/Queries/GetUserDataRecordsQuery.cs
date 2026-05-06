namespace MazyPlatform.Service.Scenario.Repository.Application.UserData.Queries;

public sealed record GetUserDataRecordsQuery(
    string ProjectId,
    string OwnerAccountId,
    string? SchemaId = null,
    int? ScenarioVersion = null,
    string? BotId = null,
    string? PlatformUserId = null,
    bool IncludeArchived = false,
    int PageSize = 50,
    int PageOffset = 0) : IQuery<GetUserDataRecordsResult>;
