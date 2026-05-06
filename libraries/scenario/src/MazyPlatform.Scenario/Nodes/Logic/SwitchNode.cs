namespace MazyPlatform.Scenario.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел множественного ветвления.
/// Проверяет значение переменной и направляет по соответствующей ветке.
/// Если совпадений нет — использует ветку "default".
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла множественного ветвления.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="variable">Имя переменной для проверки.</param>
/// <param name="cases">
/// Маппинг: значение переменной → ключ ветки.
/// Например: { "new": "case_new", "done": "case_done" }.
/// </param>
public sealed class SwitchNode(
    Guid nodeId,
    string variable,
    IReadOnlyDictionary<string, string> cases) : NodeBase(nodeId, "switch", isAwaiting: false)
{
    /// <inheritdoc />
    public override Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var value = context.Session.Variables.TryGetValue(variable, out var obj)
            ? obj?.ToString() ?? string.Empty
            : string.Empty;

        var branchKey = cases.TryGetValue(value, out var branch)
            ? branch
            : "default";

        return Task.FromResult(NodeResult.Branch(branchKey));
    }
}
