namespace MazyPlatform.Scenario.Abstractions.Scenarios;

/// <summary>
/// Резолвит платформенный сборщик графа сценария по ключу платформы.
/// </summary>
public interface IScenarioPlatformBuilderResolver
{
    /// <summary>
    /// Возвращает сборщик для указанной платформы.
    /// </summary>
    /// <param name="platformKey">Ключ платформы.</param>
    /// <returns>Платформенный сборщик.</returns>
    IScenarioPlatformBuilder Resolve(string platformKey);
}
