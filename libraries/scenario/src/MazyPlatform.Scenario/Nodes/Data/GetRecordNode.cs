namespace MazyPlatform.Scenario.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел получения записи по идентификатору.
/// Загружает данные записи в переменные сессии.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла получения записи.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="recordIdVariable">Имя переменной с RecordId.</param>
/// <param name="recordVariable">Имя переменной для сохранения данных записи.</param>
public sealed class GetRecordNode(Guid nodeId, string recordIdVariable, string recordVariable) : NodeBase(nodeId, "get_record", isAwaiting: false)
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

        var record = await context.DataStore.GetAsync(recordId, context.DataScope, cancellationToken);

        if (record is null)
            return NodeResult.Error($"Запись {recordId} не найдена.");

        context.Session.Variables[recordVariable] = record.Data;
        return NodeResult.Continue();
    }
}
