namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Commands;

public sealed record DeleteProjectCommand(string ProjectId, string OwnerAccountId) : ICommand;
