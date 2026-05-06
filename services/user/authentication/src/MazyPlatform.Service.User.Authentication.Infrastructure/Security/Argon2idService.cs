namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

using Konscious.Security.Cryptography;

using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Реализация <see cref="IPasswordHasher"/> и <see cref="ITokenHasher"/> на основе алгоритма Argon2id.
/// </summary>
/// <remarks>
/// Параметры хэширования: 3 итерации, 64 МБ памяти, параллелизм — 4 потока.
/// Формат хранимого значения: <c>salt (16 байт) | hash (32 байта)</c>, закодированный в Base64.
/// Соль генерируется случайно при каждом вызове <see cref="IPasswordHasher.Hash"/>.
/// Сравнение выполняется с постоянным временем исполнения (<see cref="CryptographicOperations.FixedTimeEquals"/>)
/// для защиты от атак по времени.
/// </remarks>
internal sealed class Argon2idService : IPasswordHasher, ITokenHasher
{
    private const int _saltSize = 16;
    private const int _hashSize = 32;
    private const int _iterations = 3;
    private const int _memorySize = 64 * 1024;
    private const int _parallelism = 4;

    /// <inheritdoc />
    public string Hash(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var salt = RandomNumberGenerator.GetBytes(_saltSize);
        var hash = HashPassword(plainText, salt);

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

            var newHash = HashPassword(plainText, salt);
            return CryptographicOperations.FixedTimeEquals(originalHash, newHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] HashPassword(string plainText, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(plainText))
        {
            Salt = salt,
            DegreeOfParallelism = _parallelism,
            MemorySize = _memorySize,
            Iterations = _iterations,
        };

        return argon2.GetBytes(_hashSize);
    }
}
