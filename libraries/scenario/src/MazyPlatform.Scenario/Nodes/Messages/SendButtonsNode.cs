namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел отправки сообщения с кнопками.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла отправки кнопок.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="text">Текст сообщения.</param>
/// <param name="buttons">Описания кнопок.</param>
/// <param name="sendButtonsUseCase">Юзкейс отправки кнопок.</param>
/// <param name="messageIdVariable">Переменная для сохранения ID отправленного сообщения (опционально).</param>
public sealed class SendButtonsNode(
    Guid nodeId,
    string text,
    ButtonLayout buttons,
    ISendButtonsUseCase sendButtonsUseCase,
    string? messageIdVariable = null) : NodeBase(nodeId, "send_buttons", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedText = context.ResolveVariables(text);
        var resolvedButtons = ButtonVariableResolver.ResolveLabels(context, buttons);
        var sendResult = await sendButtonsUseCase.ExecuteAsync(context, resolvedText, resolvedButtons, cancellationToken);

        if (messageIdVariable is not null && sendResult.MessageId is not null)
        {
            context.Session.Variables[messageIdVariable] = sendResult.MessageId;
        }

        return NodeResult.Continue(sendResult.Action);
    }
}
