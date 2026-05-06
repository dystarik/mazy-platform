namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Семантический стиль кнопки, который платформы мапят на свои цвета.
/// </summary>
public enum ButtonStyle
{
    /// <summary>
    /// Основная кнопка.
    /// </summary>
    Primary,

    /// <summary>
    /// Вторичная или нейтральная кнопка.
    /// </summary>
    Secondary,

    /// <summary>
    /// Положительное действие.
    /// </summary>
    Success,

    /// <summary>
    /// Опасное или отменяющее действие.
    /// </summary>
    Danger,
}
