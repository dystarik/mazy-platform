namespace MazyPlatform.Scenario.Nodes.Data;

using System.Globalization;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел поиска записей по фильтру.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла поиска записей.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="entityName">Имя сущности.</param>
/// <param name="recordsVariable">Имя переменной для сохранения списка найденных записей.</param>
/// <param name="filterMappings">Фильтр: имя поля → значение или шаблон (опционально).</param>
/// <param name="countVariable">
/// Имя переменной для сохранения количества найденных записей как строки в
/// <see cref="CultureInfo.InvariantCulture"/> (опционально). Полезно для
/// последующей проверки в Condition-узле без чтения самого массива.
/// </param>
public sealed class QueryRecordsNode(
    Guid nodeId,
    string entityName,
    string recordsVariable,
    IReadOnlyDictionary<string, string>? filterMappings = null,
    string? countVariable = null) : NodeBase(nodeId, "query_records", isAwaiting: false)
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

        IReadOnlyDictionary<string, object?>? filter = null;

        if (filterMappings is { Count: > 0 })
        {
            var resolved = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var (fieldName, valueTemplate) in filterMappings)
            {
                resolved[fieldName] = context.ResolveVariables(valueTemplate);
            }

            filter = resolved;
        }

        var records = await context.DataStore.QueryAsync(schema.SchemaId, context.DataScope, filter, cancellationToken);

        var resultData = records
            .Select(r => r.Data)
            .ToList();

        context.Session.Variables[recordsVariable] = resultData;

        if (countVariable is not null)
        {
            context.Session.Variables[countVariable] = resultData.Count.ToString(CultureInfo.InvariantCulture);
        }

        return NodeResult.Continue();
    }
}
