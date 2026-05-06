namespace MazyPlatform.Scenario.Abstractions.Scenarios;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Движок выполнения сценария.
/// Реактивная модель: получает событие → выполняет узлы → возвращает действия.
/// </summary>
public interface IScenarioExecutor
{
    /// <summary>
    /// Выполняет один цикл сценария.
    /// </summary>
    /// <param name="graph">Граф сценария.</param>
    /// <param name="incomingEvent">Входящее событие от мессенджера.</param>
    /// <param name="session">Текущая сессия.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="botToken">Токен бота для вызовов платформенного API.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат выполнения цикла.</returns>
    Task<ExecutionResult> ExecuteAsync(
        ScenarioGraph graph,
        IIncomingEvent incomingEvent,
        ISession session,
        Guid projectId,
        string botToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет один цикл сценария для конкретной версии релиза.
    /// </summary>
    /// <param name="graph">Граф сценария.</param>
    /// <param name="incomingEvent">Входящее событие от мессенджера.</param>
    /// <param name="session">Текущая сессия.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="scenarioVersion">Версия сценария.</param>
    /// <param name="botToken">Токен бота для вызовов платформенного API.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат выполнения цикла.</returns>
    Task<ExecutionResult> ExecuteAsync(
        ScenarioGraph graph,
        IIncomingEvent incomingEvent,
        ISession session,
        Guid projectId,
        int scenarioVersion,
        string botToken,
        CancellationToken cancellationToken = default);
}
