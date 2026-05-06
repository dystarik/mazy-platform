namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Определяет контракт для хэширования и верификации одноразовых кодов OTP.
/// </summary>
/// <remarks>
/// Используется в <see cref="OneTimePassword.Create"/> для сохранения кода в виде хэша
/// и в <see cref="OneTimePassword.Verify"/> для его последующей проверки.
/// </remarks>
public interface ICodeHasher
{
    /// <summary>
    /// Вычисляет хэш открытого кода OTP.
    /// </summary>
    /// <param name="code">Открытый числовой код для хэширования.</param>
    /// <returns>Строка хэша для сохранения в <see cref="OtpCodeHash"/>.</returns>
    string Hash(string code);

    /// <summary>
    /// Проверяет соответствие открытого кода ранее вычисленному хэшу.
    /// </summary>
    /// <param name="code">Открытый код, введённый пользователем.</param>
    /// <param name="hashedCode">Хэш кода из базы данных.</param>
    /// <returns><see langword="true"/>, если коды совпадают; иначе <see langword="false"/>.</returns>
    bool Verify(string code, string hashedCode);
}
