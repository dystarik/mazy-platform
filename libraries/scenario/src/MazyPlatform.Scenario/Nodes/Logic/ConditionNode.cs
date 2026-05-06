namespace MazyPlatform.Scenario.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Expressions;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел условного ветвления.
/// Сравнивает два значения по указанному оператору и направляет выполнение
/// по ветке "true" или "false".
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла условия.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="leftOperand">Левый операнд (может содержать шаблоны переменных).</param>
/// <param name="comparisonOperator">Оператор сравнения (см. <see cref="ConditionEvaluator"/>).</param>
/// <param name="rightOperand">Правый операнд (может содержать шаблоны переменных).</param>
public sealed class ConditionNode(
    Guid nodeId,
    string leftOperand,
    string comparisonOperator,
    string rightOperand) : NodeBase(nodeId, "condition", isAwaiting: false)
{
    /// <inheritdoc />
    public override Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedLeft = context.ResolveVariables(leftOperand);
        var resolvedRight = context.ResolveVariables(rightOperand);
        var result = ConditionEvaluator.Evaluate(resolvedLeft, comparisonOperator, resolvedRight);
        var branchKey = result ? "true" : "false";
        return Task.FromResult(NodeResult.Branch(branchKey));
    }
}
