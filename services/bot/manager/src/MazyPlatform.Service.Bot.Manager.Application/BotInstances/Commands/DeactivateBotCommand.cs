namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

public sealed record DeactivateBotCommand(string BotInstanceId, string OwnerAccountId) : ICommand;
