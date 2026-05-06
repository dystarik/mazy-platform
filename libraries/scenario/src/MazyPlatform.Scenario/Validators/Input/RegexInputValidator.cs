namespace MazyPlatform.Scenario.Validators.Input;

using System.Text.RegularExpressions;

/// <summary>
/// Валидатор по регулярному выражению.
/// Требует параметр <c>pattern</c> с паттерном регулярного выражения.
/// </summary>
public sealed class RegexInputValidator : InputValidatorBase<RegexValidatorParams>
{
    /// <inheritdoc />
    public override string ValidatorType => "regex";

    /// <inheritdoc />
    protected override RegexValidatorParams ParseParams(IReadOnlyDictionary<string, string> raw) =>
        new(raw.TryGetValue("pattern", out var pattern) ? pattern : null);

    /// <inheritdoc />
    protected override bool ValidateTyped(string input, RegexValidatorParams parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(input.Trim(), parameters.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
