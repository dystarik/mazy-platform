namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

internal sealed class AesGcmService(IOptions<BotTokenEncryptionOptions> encryptionOptions) : IBotTokenEncryptor
{
    private const int _nonceSize = 12;
    private const int _tagSize = 16;

    private readonly byte[] _encryptionKey = Convert.FromBase64String(encryptionOptions.Value.Key);

    string IBotTokenEncryptor.Decrypt(string ciphertext) => Decrypt(ciphertext, _encryptionKey);

    string IBotTokenEncryptor.Encrypt(string plaintext) => Encrypt(plaintext, _encryptionKey);

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
