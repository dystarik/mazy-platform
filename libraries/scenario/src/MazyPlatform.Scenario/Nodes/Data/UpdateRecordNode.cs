namespace MazyPlatform.Scenario.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел обновления записи пользовательской сущности.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла обновления записи.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="recordIdVariable">Имя переменной с RecordId.</param>
/// <param name="fieldMappings">Маппинг: имя поля → новое значение или шаблон.</param>
public sealed class UpdateRecordNode(
    Guid nodeId,
    string recordIdVariable,
    IReadOnlyDictionary<string, string> fieldMappings) : NodeBase(nodeId, "update_record", isAwaiting: false)
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

        var data = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (fieldName, valueTemplate) in fieldMappings)
        {
            var resolvedValue = context.ResolveVariables(valueTemplate);
            data[fieldName] = resolvedValue;
        }

        await context.DataStore.UpdateAsync(recordId, data, context.DataScope, cancellationToken);
        return NodeResult.Continue();
    }
}
