namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

public sealed record UpdateScenarioDraftCommand(string ProjectId, string OwnerAccountId, string GraphJson) : ICommand;
