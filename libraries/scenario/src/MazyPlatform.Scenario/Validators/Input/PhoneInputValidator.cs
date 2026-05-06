namespace MazyPlatform.Scenario.Validators.Input;

/// <summary>
/// Валидатор телефонного номера.
/// Поддерживает распространенные форматы с пробелами, скобками,
/// дефисами и точками. Проверяет только общий диапазон цифр.
/// </summary>
public sealed class PhoneInputValidator : InputValidatorBase<PhoneValidatorParams>
{
    private const int MinDigits = 7;
    private const int MaxDigits = 15;

    /// <inheritdoc />
    public override string ValidatorType => "phone";

    /// <inheritdoc />
    protected override PhoneValidatorParams ParseParams(IReadOnlyDictionary<string, string> raw) =>
        new();

    /// <inheritdoc />
    protected override bool ValidateTyped(string input, PhoneValidatorParams parameters)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        var digitCount = 0;

        for (var i = 0; i < trimmed.Length; i++)
        {
            var character = trimmed[i];

            if (character is >= '0' and <= '9')
            {
                digitCount++;
                continue;
            }

            if (character == '+')
            {
                if (i != 0)
                {
                    return false;
                }

                continue;
            }

            if (IsFormattingCharacter(character))
            {
                continue;
            }

            return false;
        }

        return digitCount is >= MinDigits and <= MaxDigits;
    }

    private static bool IsFormattingCharacter(char character) =>
        char.IsWhiteSpace(character) || character is '(' or ')' or '-' or '.';
}
