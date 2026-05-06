namespace MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Состояние сессии диалога.
/// </summary>
public enum SessionState
{
    /// <summary>
    /// Движок выполняет узлы.
    /// Транзитное состояние, существует только во время цикла.
    /// </summary>
    Active,

    /// <summary>
    /// Сессия остановлена на ожидающем узле.
    /// Ждёт следующего события от пользователя.
    /// </summary>
    WaitingForEvent,

    /// <summary>
    /// Сценарий завершён (дошли до конца графа).
    /// </summary>
    Completed,
}
