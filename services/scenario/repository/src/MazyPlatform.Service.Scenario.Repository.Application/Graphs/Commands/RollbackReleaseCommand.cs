namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

public sealed record RollbackReleaseCommand(string ProjectId, string OwnerAccountId, int TargetVersion) : ICommand;
