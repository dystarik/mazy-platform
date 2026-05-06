namespace MazyPlatform.Scenario.Abstractions.Scenarios.Validation;

/// <summary>
/// Ошибка валидации сценария.
/// </summary>
/// <param name="Code">Стабильный машинно-читаемый код ошибки.</param>
/// <param name="Message">Человекочитаемое описание ошибки.</param>
/// <param name="NodeId">Идентификатор узла, если ошибка относится к конкретному узлу.</param>
/// <param name="Path">JSON-путь до некорректного значения, если его можно определить.</param>
public sealed record ScenarioValidationError(
    string Code,
    string Message,
    Guid? NodeId = null,
    string? Path = null);
