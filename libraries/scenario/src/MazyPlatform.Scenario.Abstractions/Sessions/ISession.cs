namespace MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Сессия диалога — состояние конкретного пользователя в конкретном боте.
/// </summary>
public interface ISession
{
    /// <summary>
    /// Уникальный идентификатор сессии.
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Идентификатор бота.
    /// </summary>
    Guid BotId { get; }

    /// <summary>
    /// Идентификатор пользователя на платформе мессенджера.
    /// </summary>
    string PlatformUserId { get; }

    /// <summary>
    /// Идентификатор узла, на котором остановлено выполнение.
    /// Null, если сценарий завершён или ещё не начат.
    /// </summary>
    Guid? CurrentNodeId { get; set; }

    /// <summary>
    /// Текущее состояние сессии.
    /// </summary>
    SessionState State { get; set; }

    /// <summary>
    /// Переменные сценария. Наполняются по ходу выполнения.
    /// </summary>
    IDictionary<string, object?> Variables { get; }

    /// <summary>
    /// Дата создания сессии.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Дата последнего обновления.
    /// </summary>
    DateTime UpdatedAt { get; set; }
}
