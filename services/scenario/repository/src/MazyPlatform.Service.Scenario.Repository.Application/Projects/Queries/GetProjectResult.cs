namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

public sealed record GetProjectResult(Guid ProjectId, string Name, PlatformType PlatformType);
