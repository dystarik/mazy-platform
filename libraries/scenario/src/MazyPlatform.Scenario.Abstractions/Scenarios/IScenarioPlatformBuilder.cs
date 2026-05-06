namespace MazyPlatform.Scenario.Abstractions.Scenarios;

using MazyPlatform.Scenario.Abstractions.Graph;

/// <summary>
/// Сборщик графа сценария для конкретной платформы.
/// </summary>
public interface IScenarioPlatformBuilder
{
    /// <summary>
    /// Ключ платформы, для которой работает сборщик.
    /// </summary>
    string PlatformKey { get; }

    /// <summary>
    /// Собирает граф сценария из JSON с платформенными юзкейсами.
    /// </summary>
    /// <param name="scenarioJson">JSON графа сценария.</param>
    /// <returns>Собранный граф сценария.</returns>
    ScenarioGraph Build(string scenarioJson);
}
