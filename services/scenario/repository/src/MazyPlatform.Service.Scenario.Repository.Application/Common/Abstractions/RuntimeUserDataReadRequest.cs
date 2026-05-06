namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

public sealed record RuntimeUserDataReadRequest(
    Guid ProjectId,
    Guid? SchemaId,
    int? ScenarioVersion,
    Guid? BotId,
    string? PlatformUserId,
    bool IncludeArchived,
    int PageSize,
    int PageOffset);
