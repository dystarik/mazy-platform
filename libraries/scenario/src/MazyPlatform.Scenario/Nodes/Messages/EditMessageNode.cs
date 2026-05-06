namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел редактирования отправленного сообщения.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла редактирования сообщения.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="messageIdVariable">Имя переменной сессии с идентификатором сообщения.</param>
/// <param name="newText">Новый текст сообщения.</param>
/// <param name="buttons">Новый набор кнопок (опционально). Если null — кнопки не меняются.</param>
/// <param name="editMessageUseCase">Юзкейс редактирования.</param>
public sealed class EditMessageNode(
    Guid nodeId,
    string messageIdVariable,
    string newText,
    ButtonLayout? buttons,
    IEditMessageUseCase editMessageUseCase) : NodeBase(nodeId, "edit_message", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Session.Variables.TryGetValue(messageIdVariable, out var messageIdObj)
            || messageIdObj is not string messageId
            || string.IsNullOrEmpty(messageId))
        {
            return NodeResult.Continue();
        }

        var resolvedText = context.ResolveVariables(newText);
        var resolvedButtons = buttons is null ? null : ButtonVariableResolver.ResolveLabels(context, buttons);
        var action = await editMessageUseCase.ExecuteAsync(context, messageId, resolvedText, resolvedButtons, cancellationToken);
        return NodeResult.Continue(action);
    }
}
