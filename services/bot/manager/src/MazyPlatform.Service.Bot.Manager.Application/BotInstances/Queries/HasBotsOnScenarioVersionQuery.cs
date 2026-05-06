namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

public sealed record HasBotsOnScenarioVersionQuery(string ProjectId, int ScenarioVersion) : IQuery<bool>;
