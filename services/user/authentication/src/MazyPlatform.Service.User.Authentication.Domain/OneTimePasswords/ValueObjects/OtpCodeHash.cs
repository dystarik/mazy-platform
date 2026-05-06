namespace MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Объект-значение хэша одноразового кода OTP.
/// </summary>
/// <remarks>
/// Хранится в <see cref="OneTimePassword.Code"/> вместо открытого числового кода.
/// При создании через <see cref="Create"/> формат строки проверяется через
/// <see cref="HashValidator.IsValid"/>.
/// Метод <see cref="FromTrusted"/> предназначен для доверенных источников и
/// не выполняет проверку формата.
/// </remarks>
/// <seealso cref="ICodeHasher"/>
public sealed record OtpCodeHash
{
    private OtpCodeHash() { }

    /// <summary>
    /// Строка хэша одноразового кода.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Создаёт объект-значение из строки хэша, предварительно валидируя её формат.
    /// </summary>
    /// <param name="codeHash">Строка хэша, вычисленная через <see cref="ICodeHasher.Hash"/>.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с новым <see cref="OtpCodeHash"/> при успехе;
    /// сбой с кодом <see cref="ErrorCodes.Auth.InternalError"/>, если формат строки не распознан.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="codeHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static Result<OtpCodeHash> Create(string codeHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        if (!HashValidator.IsValid(codeHash))
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");

        return new OtpCodeHash { Value = codeHash };
    }

    /// <summary>
    /// Создаёт объект-значение из доверенной строки хэша без проверки формата.
    /// </summary>
    /// <param name="codeHash">Доверенная строка хэша одноразового кода.</param>
    /// <returns>Экземпляр <see cref="OtpCodeHash"/> с переданным значением.</returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="codeHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static OtpCodeHash FromTrusted(string codeHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        return new OtpCodeHash { Value = codeHash };
    }
}
