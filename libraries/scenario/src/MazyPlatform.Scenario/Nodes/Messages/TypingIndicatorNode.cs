namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел показа индикатора "бот печатает...".
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла индикатора набора текста.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="typingIndicatorUseCase">Юзкейс показа индикатора.</param>
public sealed class TypingIndicatorNode(Guid nodeId, ITypingIndicatorUseCase typingIndicatorUseCase) : NodeBase(nodeId, "typing_indicator", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var action = await typingIndicatorUseCase.ExecuteAsync(context, cancellationToken);
        return NodeResult.Continue(action);
    }
}
