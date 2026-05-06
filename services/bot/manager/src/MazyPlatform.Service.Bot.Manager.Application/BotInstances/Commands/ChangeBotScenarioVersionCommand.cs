namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

public sealed record ChangeBotScenarioVersionCommand(
    string BotInstanceId,
    string OwnerAccountId,
    int NewScenarioVersion) : ICommand;
