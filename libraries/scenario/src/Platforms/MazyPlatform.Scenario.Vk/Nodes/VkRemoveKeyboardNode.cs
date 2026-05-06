namespace MazyPlatform.Scenario.Vk.Nodes;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;
using MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Узел удаления reply-клавиатуры VK.
/// Отправляет сообщение с пустой клавиатурой, скрывая ранее показанную.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла удаления клавиатуры.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="text">Текст сообщения, отправляемого вместе с удалением клавиатуры.</param>
/// <param name="removeKeyboardUseCase">Юзкейс удаления клавиатуры.</param>
/// <param name="messageIdVariable">Имя переменной для сохранения ID отправленного сообщения (опционально).</param>
public sealed class VkRemoveKeyboardNode(
    Guid nodeId,
    string text,
    VkRemoveKeyboardUseCase removeKeyboardUseCase,
    string? messageIdVariable = null) : NodeBase(nodeId, "vk_remove_keyboard", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedText = context.ResolveVariables(text);
        var sendResult = await removeKeyboardUseCase.ExecuteAsync(context, resolvedText, cancellationToken);

        if (messageIdVariable is not null && sendResult.MessageId is not null)
        {
            context.Session.Variables[messageIdVariable] = sendResult.MessageId;
        }

        return NodeResult.Continue(sendResult.Action);
    }
}
