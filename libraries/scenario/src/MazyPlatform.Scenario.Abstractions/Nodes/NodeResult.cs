namespace MazyPlatform.Scenario.Abstractions.Nodes;

using MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Результат выполнения узла.
/// </summary>
/// <param name="Status">Статус выполнения.</param>
/// <param name="BranchKey">
/// Ключ ветки перехода. Используется условными узлами
/// для указания направления ("true"/"false").
/// Для обычных узлов — null, executor использует "default".
/// </param>
/// <param name="Actions">Список исходящих действий (сообщения, кнопки).</param>
/// <param name="ErrorMessage">Сообщение об ошибке, если Status == Error.</param>
public sealed record NodeResult(
    NodeExecutionStatus Status,
    string? BranchKey = null,
    IReadOnlyList<IOutgoingAction>? Actions = null,
    string? ErrorMessage = null)
{
    /// <summary>
    /// Узел выполнен, переход к следующему.
    /// </summary>
    /// <param name="actions">Действия для отправки (опционально).</param>
    /// <returns>Результат со статусом Continue.</returns>
    public static NodeResult Continue(IReadOnlyList<IOutgoingAction>? actions = null) =>
        new(NodeExecutionStatus.Continue, Actions: actions);

    /// <summary>
    /// Узел выполнен с одним действием, переход к следующему.
    /// </summary>
    /// <param name="action">Действие для отправки.</param>
    /// <returns>Результат со статусом Continue.</returns>
    public static NodeResult Continue(IOutgoingAction action) =>
        new(NodeExecutionStatus.Continue, Actions: [action]);

    /// <summary>
    /// Переход по указанной ветке (для условных узлов).
    /// </summary>
    /// <param name="branchKey">Ключ ветки ("true" или "false").</param>
    /// <returns>Результат с указанной веткой.</returns>
    public static NodeResult Branch(string branchKey) =>
        new(NodeExecutionStatus.Continue, BranchKey: branchKey);

    /// <summary>
    /// Узел ожидает входящее событие.
    /// </summary>
    /// <returns>Результат со статусом WaitForEvent.</returns>
    public static NodeResult Wait() =>
        new(NodeExecutionStatus.WaitForEvent);

    /// <summary>
    /// Узел ожидает входящее событие, но перед этим отправляет действие
    /// (например, сообщение об ошибке валидации).
    /// </summary>
    /// <param name="action">Действие для отправки перед ожиданием.</param>
    /// <returns>Результат со статусом WaitForEvent и действием.</returns>
    public static NodeResult Wait(IOutgoingAction action) =>
        new(NodeExecutionStatus.WaitForEvent, Actions: [action]);

    /// <summary>
    /// Ошибка при выполнении узла.
    /// </summary>
    /// <param name="message">Описание ошибки.</param>
    /// <returns>Результат со статусом Error.</returns>
    public static NodeResult Error(string message) =>
        new(NodeExecutionStatus.Error, ErrorMessage: message);
}
