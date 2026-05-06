namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

public sealed record CreateProjectCommand(string OwnerAccountId, string Name, PlatformType PlatformType) : ICommand<CreateProjectResult>;
