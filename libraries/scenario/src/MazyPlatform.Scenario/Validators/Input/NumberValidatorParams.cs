namespace MazyPlatform.Scenario.Validators.Input;

/// <summary>
/// Параметры числового валидатора.
/// </summary>
/// <param name="Min">Минимальное допустимое значение (опционально). null — нижняя граница не проверяется.</param>
/// <param name="Max">Максимальное допустимое значение (опционально). null — верхняя граница не проверяется.</param>
public sealed record NumberValidatorParams(double? Min, double? Max);
