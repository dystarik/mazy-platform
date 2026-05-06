namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

public sealed record ActivateBotCommand(string BotInstanceId, string OwnerAccountId) : ICommand;
