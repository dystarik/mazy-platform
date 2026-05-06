namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

public sealed record GetBotCredentialsResult(IBotCredentials Credentials);
