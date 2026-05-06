namespace MazyPlatform.Scenario.Validators.Input;

using System.Globalization;

/// <summary>
/// Валидатор числового ввода.
/// Опциональные параметры: <c>min</c> и <c>max</c> для ограничения диапазона.
/// Если строка параметра не парсится как число — граница молча игнорируется.
/// </summary>
public sealed class NumberInputValidator : InputValidatorBase<NumberValidatorParams>
{
    /// <inheritdoc />
    public override string ValidatorType => "number";

    /// <inheritdoc />
    protected override NumberValidatorParams ParseParams(IReadOnlyDictionary<string, string> raw) =>
        new(
            Min: TryParseDouble(raw, "min"),
            Max: TryParseDouble(raw, "max"));

    /// <inheritdoc />
    protected override bool ValidateTyped(string input, NumberValidatorParams parameters)
    {
        if (!double.TryParse(input.Trim(), CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        if (parameters.Min is { } min && number < min)
        {
            return false;
        }

        if (parameters.Max is { } max && number > max)
        {
            return false;
        }

        return true;
    }

    private static double? TryParseDouble(IReadOnlyDictionary<string, string> raw, string key) =>
        raw.TryGetValue(key, out var str)
            && double.TryParse(str, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
}
