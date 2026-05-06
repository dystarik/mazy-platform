namespace MazyPlatform.Scenario.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел установки переменной в сессии.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла установки переменной.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="variable">Имя переменной.</param>
/// <param name="value">Значение (может содержать шаблоны переменных).</param>
public sealed class SetVariableNode(Guid nodeId, string variable, string value) : NodeBase(nodeId, "set_variable", isAwaiting: false)
{
    /// <inheritdoc />
    public override Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        context.Session.Variables[variable] = context.ResolveVariables(value);
        return Task.FromResult(NodeResult.Continue());
    }
}
