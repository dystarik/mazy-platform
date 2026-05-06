namespace MazyPlatform.Scenario.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел создания записи пользовательской сущности.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла создания записи.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="entityName">Имя сущности.</param>
/// <param name="fieldMappings">Маппинг: имя поля → значение или шаблон переменной.</param>
/// <param name="recordIdVariable">Переменная для сохранения RecordId (опционально).</param>
public sealed class CreateRecordNode(
    Guid nodeId,
    string entityName,
    IReadOnlyDictionary<string, string> fieldMappings,
    string? recordIdVariable = null) : NodeBase(nodeId, "create_record", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var schema = await context.SchemaStore.GetAsync(
            context.ProjectId,
            context.ScenarioVersion,
            entityName,
            cancellationToken);

        if (schema is null)
            return NodeResult.Error($"Сущность \"{entityName}\" не найдена в проекте.");

        var data = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (fieldName, valueTemplate) in fieldMappings)
        {
            data[fieldName] = context.ResolveVariables(valueTemplate);
        }

        var record = await context.DataStore.CreateAsync(
            schema.SchemaSnapshotId,
            data,
            context.DataScope,
            context.Session.SessionId,
            cancellationToken);

        if (recordIdVariable is not null)
            context.Session.Variables[recordIdVariable] = record.RecordId.ToString();

        return NodeResult.Continue();
    }
}
