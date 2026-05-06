namespace MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Тип входящего события от мессенджера.
/// </summary>
public enum IncomingEventType
{
    /// <summary>
    /// Текстовое сообщение от пользователя.
    /// </summary>
    Message,

    /// <summary>
    /// Нажатие кнопки.
    /// </summary>
    ButtonPress,

    /// <summary>
    /// Пользователь отправил изображение.
    /// </summary>
    Image,
}
