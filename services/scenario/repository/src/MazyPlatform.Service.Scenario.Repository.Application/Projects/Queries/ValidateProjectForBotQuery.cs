namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

public sealed record ValidateProjectForBotQuery(
    string ProjectId,
    string OwnerAccountId,
    PlatformType PlatformType,
    int ScenarioVersion) : ICommand;
