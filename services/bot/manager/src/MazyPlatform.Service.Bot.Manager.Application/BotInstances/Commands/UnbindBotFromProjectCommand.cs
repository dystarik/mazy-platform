namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record UnbindBotFromProjectCommand(
    string BotInstanceId,
    string OwnerAccountId) : ICommand;
