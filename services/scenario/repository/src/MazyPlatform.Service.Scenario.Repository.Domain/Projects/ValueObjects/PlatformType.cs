namespace MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

/// <summary>
/// Тип платформы, для которой предназначен сценарий (Vk, tg и Т.Д.).
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// Унверсальная платформа.
    /// </summary>
    Universal = 0,

    /// <summary>
    /// Платформа ВКонтакте.
    /// </summary>
    Vk = 1,

    /// <summary>
    /// Платформа Telegram.
    /// </summary>
    Telegram = 2,
}
