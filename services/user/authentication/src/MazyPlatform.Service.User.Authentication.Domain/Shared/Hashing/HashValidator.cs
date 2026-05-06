namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Предоставляет вспомогательные методы проверки формата хэш-строк.
/// </summary>
/// <remarks>
/// Распознаёт три формата: hex-строки, Base64-строки и bcrypt-хэши.
/// Используется объектами-значениями (<see cref="PasswordHash"/>, <see cref="OtpCodeHash"/>,
/// <see cref="BackupCodeHash"/>, <see cref="RefreshTokenHash"/>) при создании.
/// </remarks>
public static class HashValidator
{
    /// <summary>
    /// Проверяет, соответствует ли <paramref name="hash"/> одному из допустимых форматов хэша.
    /// </summary>
    /// <param name="hash">Строка для проверки.</param>
    /// <returns>
    /// <see langword="true"/>, если строка является валидной hex-, Base64- или bcrypt-строкой;
    /// <see langword="false"/> в противном случае, в том числе если строка пустая или состоит из пробелов.
    /// </returns>
    public static bool IsValid(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        var trimmed = hash.Trim();
        return IsHex(trimmed) || IsBase64(trimmed) || LooksLikeBcrypt(trimmed);
    }

    private static bool IsHex(string value)
    {
        if (value.Length is 0 || (value.Length % 2) != 0)
            return false;

        return value.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    }

    private static bool IsBase64(string value)
    {
        if (value.Length < 16 || (value.Length % 4) != 0)
            return false;

        if (!value.All(static c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/' or '='))
            return false;

        var expectedLength = (value.Length * 3) / 4;
        var buffer = expectedLength <= 256
            ? stackalloc byte[expectedLength]
            : new byte[expectedLength];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    private static bool LooksLikeBcrypt(string value)
        => value.StartsWith("$2a$", StringComparison.Ordinal)
           || value.StartsWith("$2b$", StringComparison.Ordinal)
           || value.StartsWith("$2y$", StringComparison.Ordinal);
}
