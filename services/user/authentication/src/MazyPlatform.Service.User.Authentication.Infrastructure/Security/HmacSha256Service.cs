namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Реализация <see cref="ICodeHasher"/> и <see cref="IBackupCodeHasher"/> на основе HMAC-SHA256.
/// </summary>
/// <remarks>
/// Соль генерируется случайно при каждом вызове <see cref="ICodeHasher.Hash"/>.
/// Формат хранимого значения: <c>salt (16 байт) | hash (32 байта)</c>, закодированный в Base64.
/// Сравнение выполняется с постоянным временем исполнения (<see cref="CryptographicOperations.FixedTimeEquals"/>)
/// для защиты от атак по времени.
/// </remarks>
internal sealed class HmacSha256Service : ICodeHasher, IBackupCodeHasher
{
    private const int _saltSize = 16;
    private const int _hashSize = 32;

    /// <inheritdoc />
    public string Hash(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var salt = RandomNumberGenerator.GetBytes(_saltSize);
        var hash = HashWithSalt(plainText, salt);

        var hashBytes = new byte[_saltSize + _hashSize];
        Buffer.BlockCopy(salt, 0, hashBytes, 0, _saltSize);
        Buffer.BlockCopy(hash, 0, hashBytes, _saltSize, _hashSize);

        return Convert.ToBase64String(hashBytes);
    }

    /// <inheritdoc />
    public bool Verify(string plainText, string hashedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedValue);

        try
        {
            var hashBytes = Convert.FromBase64String(hashedValue);

            if (hashBytes.Length is not _saltSize + _hashSize)
            {
                return false;
            }

            var salt = new byte[_saltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, _saltSize);

            var originalHash = new byte[_hashSize];
            Buffer.BlockCopy(hashBytes, _saltSize, originalHash, 0, _hashSize);

            var newHash = HashWithSalt(plainText, salt);
            return CryptographicOperations.FixedTimeEquals(originalHash, newHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] HashWithSalt(string plainText, byte[] salt)
    {
        using var hmac = new HMACSHA256(salt);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
    }
}
