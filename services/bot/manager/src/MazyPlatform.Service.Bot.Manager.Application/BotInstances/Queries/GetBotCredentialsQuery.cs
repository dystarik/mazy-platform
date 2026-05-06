namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

public sealed record GetBotCredentialsQuery(string BotInstanceId) : IQuery<GetBotCredentialsResult>;
