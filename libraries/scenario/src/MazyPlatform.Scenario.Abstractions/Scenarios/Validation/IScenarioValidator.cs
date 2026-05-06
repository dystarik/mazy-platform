namespace MazyPlatform.Scenario.Abstractions.Scenarios.Validation;

/// <summary>
/// Валидирует JSON сценария до сборки runtime-графа.
/// </summary>
public interface IScenarioValidator
{
    /// <summary>
    /// Проверяет JSON сценария и возвращает все найденные ошибки.
    /// </summary>
    /// <param name="scenarioJson">JSON сценария.</param>
    /// <returns>Результат валидации со списком ошибок.</returns>
    ScenarioValidationResult Validate(string scenarioJson);
}
