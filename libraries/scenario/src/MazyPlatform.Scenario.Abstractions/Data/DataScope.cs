namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Область видимости пользовательских данных текущего выполнения сценария.
/// </summary>
/// <param name="ProjectId">Идентификатор проекта.</param>
/// <param name="ScenarioVersion">Версия сценария, которая сейчас выполняется.</param>
/// <param name="BotId">Идентификатор экземпляра бота.</param>
/// <param name="PlatformUserId">Идентификатор пользователя на платформе.</param>
public sealed record DataScope(
    Guid ProjectId,
    int ScenarioVersion,
    Guid BotId,
    string PlatformUserId);
