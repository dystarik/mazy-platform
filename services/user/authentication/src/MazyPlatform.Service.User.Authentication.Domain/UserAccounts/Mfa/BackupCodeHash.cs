namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Объект-значение хэша резервного кода MFA.
/// </summary>
/// <remarks>
/// Хранится в <see cref="MfaSettings.BackupCodes"/> вместо открытого кода.
/// При создании через <see cref="Create"/> формат строки проверяется через
/// <see cref="HashValidator.IsValid"/>.
/// Метод <see cref="FromTrusted"/> предназначен для доверенных источников и
/// не выполняет проверку формата.
/// </remarks>
/// <seealso cref="BackupCode"/>
/// <seealso cref="IBackupCodeHasher"/>
public sealed record BackupCodeHash
{
    private BackupCodeHash() { }

    /// <summary>
    /// Строка хэша резервного кода.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Создаёт объект-значение из строки хэша, предварительно валидируя её формат.
    /// </summary>
    /// <param name="backupCodeHash">Строка хэша, вычисленная через <see cref="IBackupCodeHasher.Hash"/>.</param>
    /// <returns>Новый <see cref="BackupCodeHash"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="backupCodeHash"/> пустой, состоит из пробелов или имеет недопустимый формат хэша.
    /// </exception>
    public static BackupCodeHash Create(string backupCodeHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupCodeHash);
        return !HashValidator.IsValid(backupCodeHash)
            ? throw new ArgumentException("Неверный формат хэша резервного кода.", nameof(backupCodeHash))
            : new BackupCodeHash { Value = backupCodeHash };
    }

    /// <summary>
    /// Создаёт объект-значение из доверенной строки хэша без проверки формата.
    /// </summary>
    /// <param name="backupCodeHash">Доверенная строка хэша резервного кода.</param>
    /// <returns>Экземпляр <see cref="BackupCodeHash"/> с переданным значением.</returns>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="backupCodeHash"/> пустой или состоит из пробелов.
    /// </exception>
    public static BackupCodeHash FromTrusted(string backupCodeHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupCodeHash);
        return new BackupCodeHash { Value = backupCodeHash };
    }
}
