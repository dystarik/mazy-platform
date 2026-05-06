namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

using MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;

using Microsoft.Extensions.Options;

/// <summary>
/// Реализация <see cref="ITotpSecretEncryptor"/> на основе AES-GCM (симметричное шифрование с аутентификацией).
/// </summary>
/// <remarks>
/// Формат зашифрованного блока: <c>nonce (12 байт) | ciphertext | tag (16 байт)</c>, закодированный в Base64.
/// Nonce генерируется случайно при каждом вызове <see cref="ITotpSecretEncryptor.Encrypt"/>,
/// поэтому повторное шифрование одного значения даёт разные результаты.
/// Ключ берётся из <see cref="TotpEncryptionOptions.Key"/> в формате Base64 (AES-256 требует 32 байта).
/// </remarks>
internal sealed class AesGcmService(IOptions<TotpEncryptionOptions> totpOptions) : ITotpSecretEncryptor
{
    private const int _nonceSize = 12;
    private const int _tagSize = 16;

    private readonly byte[] _totpEncryptionKey = Convert.FromBase64String(totpOptions.Value.Key);

    /// <inheritdoc />
    string ITotpSecretEncryptor.Decrypt(string ciphertext) => Decrypt(ciphertext, _totpEncryptionKey);

    /// <inheritdoc />
    string ITotpSecretEncryptor.Encrypt(string plaintext) => Encrypt(plaintext, _totpEncryptionKey);

    private static string Encrypt(string plaintext, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(_nonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[_tagSize];

        using var aes = new AesGcm(key, _tagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[_nonceSize + ciphertext.Length + _tagSize];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, _nonceSize);
        tag.CopyTo(result, _nonceSize + ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    private static string Decrypt(string ciphertext, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ciphertext);

        var data = Convert.FromBase64String(ciphertext);

        var nonce = data[.._nonceSize];
        var tag = data[^_tagSize..];
        var cipher = data[_nonceSize..^_tagSize];

        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(key, _tagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
