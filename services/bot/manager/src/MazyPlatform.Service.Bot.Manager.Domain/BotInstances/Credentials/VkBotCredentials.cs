namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

/// <summary>
/// Учётные данные бота платформы ВКонтакте.
/// </summary>
public sealed record VkBotCredentials : IBotCredentials
{
    /// <summary>
    /// Инициализирует учётные данные VK-бота.
    /// </summary>
    /// <param name="accessToken">Токен доступа сообщества.</param>
    /// <param name="communityId">Идентификатор сообщества.</param>
    public VkBotCredentials(string accessToken, string communityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(communityId);
        AccessToken = accessToken;
        CommunityId = communityId;
    }

    /// <inheritdoc />
    public PlatformType PlatformType => PlatformType.Vk;

    /// <inheritdoc />
    public string AccessToken { get; }

    /// <summary>Идентификатор сообщества ВКонтакте.</summary>
    public string CommunityId { get; }
}
