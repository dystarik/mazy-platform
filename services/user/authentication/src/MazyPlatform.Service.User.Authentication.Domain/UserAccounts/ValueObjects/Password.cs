namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Объект-значение открытого пароля с комплексной политикой валидации.
/// </summary>
/// <remarks>
/// Существует только на этапе входящей обработки запроса: валидируется, затем сразу
/// хэшируется через <see cref="IPasswordHasher"/> в <see cref="PasswordHash"/>.
/// Открытый текст пароля никогда не сохраняется в агрегатах.
/// </remarks>
/// <seealso cref="PasswordHash"/>
/// <seealso cref="ErrorCodes.Auth.Password"/>
public sealed record Password
{
    /// <summary>Минимальная длина пароля в символах.</summary>
    public const int MinLength = 8;

    /// <summary>Максимальная длина пароля в символах.</summary>
    public const int MaxLength = 128;

    private Password() { }

    /// <summary>
    /// Открытый текст пароля, прошедшего валидацию.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Создаёт и валидирует объект-значение пароля.
    /// </summary>
    /// <param name="value">Открытый текст пароля для проверки.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с новым <see cref="Password"/> при успехе;
    /// сбой с набором ошибок <see cref="ErrorCodes.Auth.Password"/>, если нарушены одно или несколько требований:
    /// длина, наличие заглавных и строчных букв, цифр и специальных символов.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="value"/> пустой или состоит из пробелов.
    /// </exception>
    public static Result<Password> Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var errors = new List<Error>();

        switch (value.Length)
        {
            case < MinLength:
                errors.Add(Error.Validation(ErrorCodes.Auth.Password.TooShort, "Неверный формат пароля."));
                break;
            case > MaxLength:
                errors.Add(Error.Validation(ErrorCodes.Auth.Password.TooLong, "Неверный формат пароля."));
                break;
        }

        if (!value.Any(char.IsUpper))
            errors.Add(Error.Validation(ErrorCodes.Auth.Password.NoUpperCase, "Неверный формат пароля."));

        if (!value.Any(char.IsLower))
            errors.Add(Error.Validation(ErrorCodes.Auth.Password.NoLowerCase, "Неверный формат пароля."));

        if (!value.Any(char.IsDigit))
            errors.Add(Error.Validation(ErrorCodes.Auth.Password.NoDigit, "Неверный формат пароля."));

        if (value.All(char.IsLetterOrDigit))
            errors.Add(Error.Validation(ErrorCodes.Auth.Password.NoSpecialChar, "Неверный формат пароля."));

        if (errors.Count is not 0)
            return new ErrorCollection(errors);

        return new Password { Value = value };
    }
}
