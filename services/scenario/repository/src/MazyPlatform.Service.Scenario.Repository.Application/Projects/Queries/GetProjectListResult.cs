namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

public sealed record GetProjectListResult(IReadOnlyList<GetProjectListResult.ProjectListItem> Items)
{
    public sealed record ProjectListItem(Guid ProjectId, string Name, PlatformType PlatformType);
}
