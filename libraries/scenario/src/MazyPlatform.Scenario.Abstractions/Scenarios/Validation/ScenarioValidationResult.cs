namespace MazyPlatform.Scenario.Abstractions.Scenarios.Validation;

/// <summary>
/// Результат валидации JSON сценария.
/// </summary>
/// <param name="Errors">Список ошибок валидации.</param>
public sealed record ScenarioValidationResult(IReadOnlyList<ScenarioValidationError> Errors)
{
    /// <summary>
    /// Возвращает значение, указывающее, что сценарий не содержит ошибок валидации.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}
