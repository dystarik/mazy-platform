namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел отправки текстового сообщения.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла отправки сообщения.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="text">Текст сообщения (может содержать шаблоны переменных).</param>
/// <param name="sendMessageUseCase">Юзкейс отправки сообщения.</param>
/// <param name="messageIdVariable">Переменная для сохранения ID отправленного сообщения (опционально).</param>
public sealed class SendMessageNode(
    Guid nodeId,
    string text,
    ISendMessageUseCase sendMessageUseCase,
    string? messageIdVariable = null) : NodeBase(nodeId, "send_message", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedText = context.ResolveVariables(text);
        var sendResult = await sendMessageUseCase.ExecuteAsync(context, resolvedText, cancellationToken);

        if (messageIdVariable is not null && sendResult.MessageId is not null)
        {
            context.Session.Variables[messageIdVariable] = sendResult.MessageId;
        }

        return NodeResult.Continue(sendResult.Action);
    }
}
