namespace MazyPlatform.Scenario.Abstractions.Scenarios.Validation;

/// <summary>
/// Валидирует JSON сценария с учётом узлов, доступных для конкретной платформы.
/// </summary>
public interface IPlatformScenarioValidator
{
    /// <summary>
    /// Валидирует JSON сценария для указанной платформы.
    /// </summary>
    /// <param name="scenarioJson">JSON графа сценария.</param>
    /// <param name="platformKey">Ключ платформы.</param>
    /// <returns>Результат валидации.</returns>
    ScenarioValidationResult Validate(string scenarioJson, string platformKey);
}
