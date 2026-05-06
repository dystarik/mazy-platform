namespace MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Входящее событие от пользователя мессенджера.
/// Создаётся адаптером платформы из сырого webhook.
/// </summary>
public interface IIncomingEvent
{
    /// <summary>
    /// Тип события.
    /// </summary>
    IncomingEventType EventType { get; }

    /// <summary>
    /// Идентификатор бота, которому адресовано событие.
    /// </summary>
    Guid BotId { get; }

    /// <summary>
    /// Идентификатор пользователя на платформе мессенджера.
    /// </summary>
    string PlatformUserId { get; }

    /// <summary>
    /// Идентификатор чата/диалога на платформе.
    /// </summary>
    string ChatId { get; }

    /// <summary>
    /// Текст сообщения (для событий типа Message).
    /// </summary>
    string? Text { get; }

    /// <summary>
    /// Payload кнопки (для событий типа ButtonPress).
    /// </summary>
    string? Payload { get; }

    /// <summary>
    /// URL или идентификатор изображения (для событий типа Image).
    /// </summary>
    string? ImageUrl { get; }

    /// <summary>
    /// Подпись к изображению (для событий типа Image).
    /// </summary>
    string? ImageCaption { get; }

    /// <summary>
    /// Идентификатор сообщения на платформе.
    /// Используется для редактирования и удаления.
    /// </summary>
    string? MessageId { get; }
}
