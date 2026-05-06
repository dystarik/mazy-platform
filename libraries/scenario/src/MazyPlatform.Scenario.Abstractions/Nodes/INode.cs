namespace MazyPlatform.Scenario.Abstractions.Nodes;

using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Узел сценария — единица логики диалога.
/// </summary>
public interface INode
{
    /// <summary>
    /// Уникальный идентификатор узла в графе сценария.
    /// </summary>
    Guid NodeId { get; }

    /// <summary>
    /// Строковый тип узла (например, "send_message", "condition").
    /// Используется при сборке графа из JSON.
    /// </summary>
    string NodeType { get; }

    /// <summary>
    /// Признак того, что узел ожидает входящее событие.
    /// Используется executor-ом для определения точек остановки.
    /// </summary>
    bool IsAwaiting { get; }

    /// <summary>
    /// Выполняет логику узла.
    /// </summary>
    /// <param name="context">Контекст выполнения сценария.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат выполнения.</returns>
    Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default);
}
