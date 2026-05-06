namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Security;

internal interface IBotTokenEncryptor
{
    string Encrypt(string plaintext);

    string Decrypt(string ciphertext);
}
