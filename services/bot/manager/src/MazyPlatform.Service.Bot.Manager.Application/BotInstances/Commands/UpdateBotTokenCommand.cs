namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

public sealed record UpdateBotTokenCommand(
    string BotInstanceId,
    string OwnerAccountId,
    string AccessToken,
    string? CommunityId) : ICommand;
