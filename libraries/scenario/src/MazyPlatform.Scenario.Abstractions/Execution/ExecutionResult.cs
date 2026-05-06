namespace MazyPlatform.Scenario.Abstractions.Execution;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Результат одного цикла выполнения сценария.
/// Возвращается движком после обработки входящего события.
/// </summary>
/// <param name="Session">Обновлённая сессия.</param>
/// <param name="Actions">Действия для отправки в мессенджер.</param>
/// <param name="State">Итоговое состояние сессии.</param>
/// <param name="Errors">Ошибки, если возникли.</param>
public sealed record ExecutionResult(
    ISession Session,
    IReadOnlyList<IOutgoingAction> Actions,
    SessionState State,
    IReadOnlyList<string>? Errors = null)
{
    /// <summary>
    /// Возвращает <see langword="true"/>, если выполнение завершилось без ошибок.
    /// </summary>
    public bool IsSuccess => Errors is null or [];
}
