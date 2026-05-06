namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

/// <summary>
/// Результат проверки наличия ботов, привязанных к указанной версии сценария проекта.
/// </summary>
/// <param name="HasBots">
/// <see langword="true"/>, если хотя бы один бот привязан к указанной версии сценария;
/// иначе <see langword="false"/>.
/// </param>
public sealed record HasBotsOnScenarioVersionResult(bool HasBots);
