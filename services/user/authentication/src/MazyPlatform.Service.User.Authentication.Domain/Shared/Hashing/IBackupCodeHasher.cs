namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Определяет контракт для хэширования и верификации резервных кодов MFA.
/// </summary>
/// <remarks>
/// Используется в <see cref="MfaSettings.ConfirmMfaMethod"/> при генерации резервных кодов
/// и в <see cref="MfaSettings.UseBackupCode"/> при их использовании.
/// Резервные коды намеренно хэшируются отдельным интерфейсом, так как алгоритм
/// может отличаться от алгоритма хэширования OTP-кодов.
/// </remarks>
public interface IBackupCodeHasher
{
    /// <summary>
    /// Вычисляет хэш резервного кода.
    /// </summary>
    /// <param name="code">Открытый резервный код для хэширования.</param>
    /// <returns>Строка хэша для сохранения в <see cref="BackupCodeHash"/>.</returns>
    string Hash(string code);

    /// <summary>
    /// Проверяет соответствие открытого резервного кода ранее вычисленному хэшу.
    /// </summary>
    /// <param name="code">Открытый резервный код, введённый пользователем.</param>
    /// <param name="codeHash">Хэш кода из базы данных.</param>
    /// <returns><see langword="true"/>, если коды совпадают; иначе <see langword="false"/>.</returns>
    bool Verify(string code, string codeHash);
}
