namespace MazyPlatform.Scenario.Vk.Nodes;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;
using MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Узел отправки VK reply-клавиатуры под полем ввода.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла отправки VK-клавиатуры.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="text">Текст сообщения.</param>
/// <param name="buttonRows">Строки кнопок клавиатуры.</param>
/// <param name="oneTime">Скрывать клавиатуру после нажатия.</param>
/// <param name="sendKeyboardUseCase">Юзкейс отправки клавиатуры.</param>
/// <param name="messageIdVariable">Переменная для сохранения ID отправленного сообщения (опционально).</param>
public sealed class VkSendKeyboardNode(
    Guid nodeId,
    string text,
    IReadOnlyList<IReadOnlyList<VkButtonInfo>> buttonRows,
    bool oneTime,
    VkSendKeyboardUseCase sendKeyboardUseCase,
    string? messageIdVariable = null) : NodeBase(nodeId, "vk_send_keyboard", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedText = context.ResolveVariables(text);
        var sendResult = await sendKeyboardUseCase.ExecuteAsync(context, resolvedText, buttonRows, oneTime, cancellationToken);

        if (messageIdVariable is not null && sendResult.MessageId is not null)
        {
            context.Session.Variables[messageIdVariable] = sendResult.MessageId;
        }

        return NodeResult.Continue(sendResult.Action);
    }
}
