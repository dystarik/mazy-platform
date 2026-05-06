namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record BindBotToProjectCommand(
    string BotInstanceId,
    string OwnerAccountId,
    string ProjectId,
    int ScenarioVersion) : ICommand;
