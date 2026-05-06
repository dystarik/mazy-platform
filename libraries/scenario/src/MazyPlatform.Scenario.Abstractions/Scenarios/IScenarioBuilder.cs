namespace MazyPlatform.Scenario.Abstractions.Scenarios;

using MazyPlatform.Scenario.Abstractions.Graph;

/// <summary>
/// Собирает граф сценария из JSON.
/// </summary>
public interface IScenarioBuilder
{
    /// <summary>
    /// Строит граф сценария из JSON-строки.
    /// Использует INodeRegistry для создания узлов.
    /// </summary>
    /// <param name="scenarioJson">JSON сценария.</param>
    /// <returns>Собранный граф.</returns>
    ScenarioGraph Build(string scenarioJson);
}
