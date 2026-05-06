namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

public sealed record GetVersionHistoryResult(IReadOnlyList<GetVersionHistoryResult.VersionItem> Versions)
{
    public sealed record VersionItem(int Version, DateTimeOffset CreatedAt, bool IsCurrent);
}
