namespace MazyPlatform.Scenario.Validators.Input;

using System.Text.RegularExpressions;

/// <summary>
/// Валидатор адреса электронной почты.
/// </summary>
public sealed partial class EmailInputValidator : InputValidatorBase<EmailValidatorParams>
{
    /// <inheritdoc />
    public override string ValidatorType => "email";

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex { get; }

    /// <inheritdoc />
    protected override EmailValidatorParams ParseParams(IReadOnlyDictionary<string, string> raw) =>
        new();

    /// <inheritdoc />
    protected override bool ValidateTyped(string input, EmailValidatorParams parameters) =>
        EmailRegex.IsMatch(input.Trim());
}
