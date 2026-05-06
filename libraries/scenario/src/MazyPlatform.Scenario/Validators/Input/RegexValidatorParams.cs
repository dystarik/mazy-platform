namespace MazyPlatform.Scenario.Validators.Input;

/// <summary>
/// Параметры валидатора по регулярному выражению.
/// </summary>
/// <param name="Pattern">Паттерн регулярного выражения (обязателен — без него валидатор всегда возвращает false).</param>
public sealed record RegexValidatorParams(string? Pattern);
