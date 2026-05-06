namespace MazyPlatform.Scenario.Vk.Nodes;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;
using MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Узел отправки карусели карточек VK.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла отправки карусели.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="cards">Карточки карусели.</param>
/// <param name="sendCarouselUseCase">Юзкейс отправки карусели.</param>
/// <param name="text">Текст сообщения, отправляемого вместе с каруселью.</param>
/// <param name="messageIdVariable">Имя переменной для сохранения ID отправленного сообщения (опционально).</param>
public sealed class VkSendCarouselNode(
    Guid nodeId,
    IReadOnlyList<VkCarouselCard> cards,
    VkSendCarouselUseCase sendCarouselUseCase,
    string text,
    string? messageIdVariable = null) : NodeBase(nodeId, "vk_send_carousel", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedText = context.ResolveVariables(text);
        var sendResult = await sendCarouselUseCase.ExecuteAsync(context, cards, resolvedText, cancellationToken);

        if (messageIdVariable is not null && sendResult.MessageId is not null)
        {
            context.Session.Variables[messageIdVariable] = sendResult.MessageId;
        }

        return NodeResult.Continue(sendResult.Action);
    }
}
