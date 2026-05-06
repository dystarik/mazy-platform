namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

public sealed record GetBotsByUserIdQuery(string OwnerAccountId) : IQuery<GetBotsByUserIdResult>;
