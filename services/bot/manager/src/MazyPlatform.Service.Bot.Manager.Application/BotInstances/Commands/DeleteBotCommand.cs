namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

public sealed record DeleteBotCommand(string BotInstanceId, string OwnerAccountId) : ICommand;
