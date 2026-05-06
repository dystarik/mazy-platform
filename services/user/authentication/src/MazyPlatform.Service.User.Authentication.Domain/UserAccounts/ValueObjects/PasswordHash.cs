namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Объект-значение хэша пароля пользователя.
/// </summary>
/// <remarks>
/// Хранится в <see cref="UserAccount.PasswordHash"/> и никогда не содержит открытый текст.
/// При создании через <see cref="Create"/> формат строки проверяется через
/// <see cref="HashValidator.IsValid"/>.
/// Метод <see cref="FromTrusted"/> предназначен для доверенных источников и
/// не выполняет проверку формата.
/// </remarks>
/// <seealso cref="IPasswordHasher"/>
public sealed record PasswordHash
{
    private PasswordHash() { }

    /// <summary>
    /// Строка хэша пароля.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Создаёт объект-значение из строки хэша, предварительно валидируя её формат.
    /// </summary>
    /// <param name="passwordHash">Строка хэша, вычисленная через <see cref="IPasswordHasher.Hash"/>.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с новым <see cref="PasswordHash"/> при успехе;
    /// сбой с кодом <see cref="ErrorCodes.Auth.InternalError"/>, если формат строки не распознан.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="passwordHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static Result<PasswordHash> Create(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        if (!HashValidator.IsValid(passwordHash))
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");

        return new PasswordHash { Value = passwordHash };
    }

    /// <summary>
    /// Создаёт объект-значение из доверенной строки хэша без проверки формата.
    /// </summary>
    /// <param name="passwordHash">Доверенная строка хэша пароля.</param>
    /// <returns>Экземпляр <see cref="PasswordHash"/> с переданным значением.</returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="passwordHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static PasswordHash FromTrusted(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return new PasswordHash { Value = passwordHash };
    }
}
