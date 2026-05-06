namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация получения информации о пользователе.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkGetUserInfoUseCase(VkApiClient apiClient) : IGetUserInfoUseCase
{
    /// <inheritdoc />
    public async Task<UserInfo> GetAsync(string platformUserId, string botToken, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["user_ids"] = platformUserId,
            ["fields"] = "photo_200",
        };

        var response = await apiClient.CallAsync("users.get", parameters, botToken, cancellationToken);
        var user = response[0];

        var firstName = user.GetProperty("first_name").GetString() ?? string.Empty;
        var lastName = user.TryGetProperty("last_name", out var lastNameProp) ? lastNameProp.GetString() : null;
        var screenName = user.TryGetProperty("screen_name", out var screenNameProp) ? screenNameProp.GetString() : null;
        var avatarUrl = user.TryGetProperty("photo_200", out var photoProp) ? photoProp.GetString() : null;

        return new UserInfo(firstName, lastName, screenName, avatarUrl);
    }
}
