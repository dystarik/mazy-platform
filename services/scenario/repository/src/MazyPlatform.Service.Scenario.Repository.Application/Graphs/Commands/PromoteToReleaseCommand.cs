namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

public sealed record PromoteToReleaseCommand(string ProjectId, string OwnerAccountId) : ICommand;
