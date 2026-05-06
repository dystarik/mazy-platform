namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация получения информации о пользователе.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramGetUserInfoUseCase(TelegramApiClient apiClient) : IGetUserInfoUseCase
{
    /// <inheritdoc />
    public async Task<UserInfo> GetAsync(string platformUserId, string botToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var chat = await apiClient.CallAsync(
                "getChat",
                new { chat_id = platformUserId },
                botToken,
                cancellationToken);

            var firstName = chat.TryGetProperty("first_name", out var firstNameElement)
                ? firstNameElement.GetString() ?? platformUserId
                : platformUserId;
            var lastName = chat.TryGetProperty("last_name", out var lastNameElement)
                ? lastNameElement.GetString()
                : null;
            var username = chat.TryGetProperty("username", out var usernameElement)
                ? usernameElement.GetString()
                : null;
            var avatarUrl = await TryGetAvatarUrlAsync(chat, botToken, cancellationToken);

            return new UserInfo(firstName, lastName, username, avatarUrl);
        }
        catch (TelegramApiException)
        {
            return new UserInfo(platformUserId);
        }
    }

    private async Task<string?> TryGetAvatarUrlAsync(System.Text.Json.JsonElement chat, string botToken, CancellationToken cancellationToken)
    {
        if (!chat.TryGetProperty("photo", out var photo)
            || !photo.TryGetProperty("big_file_id", out var bigFileIdElement)
            || bigFileIdElement.GetString() is not { Length: > 0 } bigFileId)
        {
            return null;
        }

        var file = await apiClient.CallAsync(
            "getFile",
            new { file_id = bigFileId },
            botToken,
            cancellationToken);

        return file.TryGetProperty("file_path", out var filePathElement)
            && filePathElement.GetString() is { Length: > 0 } filePath
            ? apiClient.GetFileUrl(botToken, filePath)
            : null;
    }
}
