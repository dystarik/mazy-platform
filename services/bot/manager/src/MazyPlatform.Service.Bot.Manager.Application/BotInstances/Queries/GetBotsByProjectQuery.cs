namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

public sealed record GetBotsByProjectQuery(string ProjectId, string OwnerAccountId) : IQuery<GetBotsByProjectResult>;
