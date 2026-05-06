namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

public sealed record ChangeBotScenarioVersionUpdateModeCommand(
    string BotInstanceId,
    string OwnerAccountId,
    ScenarioVersionUpdateMode ScenarioVersionUpdateMode) : ICommand;
