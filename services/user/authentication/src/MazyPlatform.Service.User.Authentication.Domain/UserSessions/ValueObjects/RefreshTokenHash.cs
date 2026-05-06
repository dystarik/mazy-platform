namespace MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

/// <summary>
/// Объект-значение хэша токена обновления сессии.
/// </summary>
/// <remarks>
/// Хранится в <see cref="UserSession.RefreshTokenHash"/> вместо открытого токена.
/// При создании через <see cref="Create"/> формат строки проверяется через
/// <see cref="HashValidator.IsValid"/>.
/// Метод <see cref="FromTrusted"/> предназначен для доверенных источников и
/// не выполняет проверку формата.
/// </remarks>
/// <seealso cref="ITokenHasher"/>
public sealed record RefreshTokenHash
{
    private RefreshTokenHash() { }

    /// <summary>
    /// Строка хэша токена обновления.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Создаёт объект-значение из строки хэша, предварительно валидируя её формат.
    /// </summary>
    /// <param name="refreshTokenHash">Строка хэша, вычисленная через <see cref="ITokenHasher.Hash"/>.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с новым <see cref="RefreshTokenHash"/> при успехе;
    /// сбой с кодом <see cref="ErrorCodes.Auth.InternalError"/>, если формат строки не распознан.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="refreshTokenHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static Result<RefreshTokenHash> Create(string refreshTokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);

        if (!HashValidator.IsValid(refreshTokenHash))
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");

        return new RefreshTokenHash { Value = refreshTokenHash };
    }

    /// <summary>
    /// Создаёт объект-значение из доверенной строки хэша без проверки формата.
    /// </summary>
    /// <param name="refreshTokenHash">Доверенная строка хэша токена обновления.</param>
    /// <returns>Экземпляр <see cref="RefreshTokenHash"/> с переданным значением.</returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="refreshTokenHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static RefreshTokenHash FromTrusted(string refreshTokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);
        return new RefreshTokenHash { Value = refreshTokenHash };
    }
}
