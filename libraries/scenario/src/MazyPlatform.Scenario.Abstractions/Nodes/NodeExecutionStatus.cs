namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Статус выполнения узла.
/// </summary>
public enum NodeExecutionStatus
{
    /// <summary>
    /// Узел выполнен, можно переходить к следующему.
    /// </summary>
    Continue,

    /// <summary>
    /// Узел ожидает входящее событие от пользователя.
    /// Движок сохраняет сессию и завершает текущий цикл.
    /// </summary>
    WaitForEvent,

    /// <summary>
    /// Ошибка при выполнении узла.
    /// </summary>
    Error,
}
