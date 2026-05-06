namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

/// <summary>
/// Маркерный интерфейс для учётных данных бота конкретной платформы.
/// </summary>
public interface IBotCredentials
{
    /// <summary>Тип платформы.</summary>
    PlatformType PlatformType { get; }

    /// <summary>Токен доступа.</summary>
    string AccessToken { get; }
}
