namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using System.Globalization;
using System.Security.Cryptography;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Объект-значение открытого резервного кода MFA.
/// </summary>
/// <remarks>
/// Коды генерируются через <see cref="GenerateBackupCodes"/> и сразу хэшируются через
/// <see cref="IBackupCodeHasher"/> в <see cref="BackupCodeHash"/> для хранения в базе.
/// Открытый код возвращается пользователю единственный раз.
/// </remarks>
/// <seealso cref="BackupCodeHash"/>
/// <seealso cref="MfaSettings"/>
public sealed record BackupCode
{
    /// <summary>Длина резервного кода в символах (hex-строка из 3 байт).</summary>
    public const int Length = 6;

    private BackupCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Резервный код не может быть пустым или состоять только из пробелов.", nameof(code));

        if (code.Length < Length)
            throw new ArgumentException($"Резервный код должен быть длиной {Length} символов.", nameof(code));

        Code = code;
    }

    /// <summary>
    /// Открытый шестисимвольный резервный код в нижнем регистре.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Создаёт объект-значение из существующего кода с валидацией длины.
    /// </summary>
    /// <param name="code">Строка кода длиной ровно <see cref="Length"/> символов.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с новым <see cref="BackupCode"/> при успехе;
    /// сбой с кодом <see cref="ErrorCodes.Auth.Validation.Invalid"/>, если длина кода не равна <see cref="Length"/>.
    /// </returns>
    public static Result<BackupCode> Create(string code)
    {
        if (code.Length is not Length)
            return Error.Validation(ErrorCodes.Auth.Validation.Invalid, "Неверный формат резервного кода.");

        return new BackupCode(code);
    }

    /// <summary>
    /// Генерирует набор криптографически стойких резервных кодов.
    /// </summary>
    /// <param name="count">Количество кодов для генерации. Должно быть больше нуля.</param>
    /// <returns>
    /// Коллекция из <paramref name="count"/> уникальных <see cref="BackupCode"/>,
    /// каждый из которых представляет собой шестисимвольную hex-строку.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="count"/> не является положительным числом.
    /// </exception>
    public static IReadOnlyCollection<BackupCode> GenerateBackupCodes(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Количество резервных кодов должно быть положительным числом.", nameof(count));

        var backupCodes = new List<BackupCode>(count);
        for (var i = 0; i < count; i++)
        {
            var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(Length / 2)).ToLower(CultureInfo.InvariantCulture);
            backupCodes.Add(new BackupCode(code));
        }

        return backupCodes.AsReadOnly();
    }
}
