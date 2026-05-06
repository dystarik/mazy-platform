namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Определяет контракт для хэширования и верификации паролей пользователей.
/// </summary>
/// <remarks>
/// Реализация внедряется инфраструктурным слоем. Домен использует интерфейс
/// напрямую в доменных сервисах (<see cref="RegisterByPasswordService"/>, <see cref="LoginByPasswordService"/> и др.)
/// и никогда не хранит пароль в открытом виде.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Вычисляет хэш открытого пароля.
    /// </summary>
    /// <param name="password">Открытый пароль для хэширования.</param>
    /// <returns>Строка хэша, пригодная для сохранения в <see cref="PasswordHash"/>.</returns>
    string Hash(string password);

    /// <summary>
    /// Проверяет соответствие открытого пароля ранее вычисленному хэшу.
    /// </summary>
    /// <param name="password">Открытый пароль, введённый пользователем.</param>
    /// <param name="hashedPassword">Хэш, сохранённый в базе данных.</param>
    /// <returns><see langword="true"/>, если пароль совпадает; иначе <see langword="false"/>.</returns>
    bool Verify(string password, string hashedPassword);
}
