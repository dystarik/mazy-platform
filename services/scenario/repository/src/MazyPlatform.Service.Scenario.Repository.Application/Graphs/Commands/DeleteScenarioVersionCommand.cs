namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

public sealed record DeleteScenarioVersionCommand(
    string ProjectId,
    string OwnerAccountId,
    int Version) : ICommand;
