namespace MazyPlatform.Scenario.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел удаления записи.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла удаления записи.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="recordIdVariable">Имя переменной с RecordId.</param>
public sealed class DeleteRecordNode(Guid nodeId, string recordIdVariable) : NodeBase(nodeId, "delete_record", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Session.Variables.TryGetValue(recordIdVariable, out var idObj)
            || idObj is not string idStr
            || !Guid.TryParse(idStr, out var recordId))
        {
            return NodeResult.Error($"Переменная \"{recordIdVariable}\" не содержит корректный RecordId.");
        }

        await context.DataStore.DeleteAsync(recordId, context.DataScope, cancellationToken);
        return NodeResult.Continue();
    }
}
