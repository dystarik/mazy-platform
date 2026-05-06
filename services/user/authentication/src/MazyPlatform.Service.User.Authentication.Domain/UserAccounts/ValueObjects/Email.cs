namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using System.Text.RegularExpressions;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Объект-значение email-адреса с встроенной валидацией формата.
/// </summary>
/// <remarks>
/// Хранит адрес в нижнем регистре (<see cref="string.ToLowerInvariant"/>).
/// При создании через <see cref="Create"/> выполняется проверка длины и формата.
/// Метод <see cref="FromTrusted"/> предназначен для доверенных источников и
/// не выполняет валидацию, но нормализует значение в нижний регистр.
/// </remarks>
/// <seealso cref="ErrorCodes.Auth.Email"/>
public sealed partial record Email
{
    /// <summary>Минимальная допустимая длина email-адреса в символах.</summary>
    public const int MinLength = 5;

    /// <summary>Максимальная допустимая длина email-адреса в символах (RFC 5321).</summary>
    public const int MaxLength = 254;

    private Email() { }

    /// <summary>
    /// Нормализованный email-адрес в нижнем регистре.
    /// </summary>
    public required string Value { get; init; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+(?>\.[^@\s]+)+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, 200)]
    private static partial Regex SimpleEmailRegex { get; }

    /// <summary>
    /// Создаёт и валидирует объект-значение email.
    /// </summary>
    /// <param name="email">Строка email-адреса для проверки.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с новым <see cref="Email"/> при успехе;
    /// сбой с набором ошибок <see cref="ErrorCodes.Auth.Email"/>, если длина или формат не соответствуют требованиям.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="email"/> пустой или состоит из пробелов.
    /// </exception>
    public static Result<Email> Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var errors = new List<Error>();

        switch (email.Length)
        {
            case < MinLength:
                errors.Add(Error.Validation(ErrorCodes.Auth.Email.TooShort, "Email должен быть не менее 5 символов."));
                break;
            case > MaxLength:
                errors.Add(Error.Validation(ErrorCodes.Auth.Email.TooLong, "Email должен быть не более 254 символов."));
                break;
        }

        if (!SimpleEmailRegex.IsMatch(email))
            errors.Add(Error.Validation(ErrorCodes.Auth.Email.InvalidFormat, "Email имеет недопустимый формат."));

        if (errors.Count is not 0)
            return new ErrorCollection(errors);

        return new Email { Value = email.ToLowerInvariant() };
    }

    /// <summary>
    /// Создаёт объект-значение из доверенной строки email без проверки длины и формата.
    /// </summary>
    /// <param name="email">Доверенная строка email-адреса.</param>
    /// <returns>Экземпляр <see cref="Email"/> с адресом в нижнем регистре.</returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="email"/> пустой или состоит из пробелов.
    /// </exception>
    public static Email FromTrusted(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new Email { Value = email.ToLowerInvariant() };
    }
}
