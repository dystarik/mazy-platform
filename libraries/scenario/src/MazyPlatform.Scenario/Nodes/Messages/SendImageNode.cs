namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел отправки изображения.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла отправки изображения.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="imageUrl">URL изображения (может содержать шаблоны переменных).</param>
/// <param name="sendImageUseCase">Юзкейс отправки изображения.</param>
/// <param name="caption">Подпись (опционально, может содержать шаблоны).</param>
/// <param name="messageIdVariable">Переменная для сохранения ID отправленного сообщения (опционально).</param>
public sealed class SendImageNode(
    Guid nodeId,
    string imageUrl,
    ISendImageUseCase sendImageUseCase,
    string? caption = null,
    string? messageIdVariable = null) : NodeBase(nodeId, "send_image", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedUrl = context.ResolveVariables(imageUrl);
        var resolvedCaption = caption is not null ? context.ResolveVariables(caption) : null;
        var sendResult = await sendImageUseCase.ExecuteAsync(context, resolvedUrl, resolvedCaption, cancellationToken);

        if (messageIdVariable is not null && sendResult.MessageId is not null)
        {
            context.Session.Variables[messageIdVariable] = sendResult.MessageId;
        }

        return NodeResult.Continue(sendResult.Action);
    }
}
