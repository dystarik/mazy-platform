namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.StreamingQueries;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

public sealed record ActiveBotItem(Guid BotInstanceId, Guid ProjectId, int ScenarioVersion, IBotCredentials Credentials);
