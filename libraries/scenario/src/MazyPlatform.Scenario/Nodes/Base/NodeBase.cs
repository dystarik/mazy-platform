namespace MazyPlatform.Scenario.Nodes.Base;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Базовый класс узла сценария.
/// Избавляет наследников от повторения общих свойств.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла.
/// </remarks>
/// <param name="nodeId">Уникальный идентификатор узла в графе.</param>
/// <param name="nodeType">Строковый тип узла.</param>
/// <param name="isAwaiting">Признак ожидающего узла.</param>
public abstract class NodeBase(Guid nodeId, string nodeType, bool isAwaiting) : INode
{
    /// <inheritdoc />
    public Guid NodeId { get; } = nodeId;

    /// <inheritdoc />
    public string NodeType { get; } = nodeType;

    /// <inheritdoc />
    public bool IsAwaiting { get; } = isAwaiting;

    /// <inheritdoc />
    public abstract Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default);
}
