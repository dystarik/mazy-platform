namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация удаления сообщения.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkDeleteMessageUseCase(VkApiClient apiClient) : IDeleteMessageUseCase
{
    /// <inheritdoc />
    public async Task<IOutgoingAction> ExecuteAsync(ExecutionContext context, string messageId, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["message_ids"] = messageId,
            ["delete_for_all"] = "1",
            ["peer_id"] = context.IncomingEvent!.ChatId,
        };

        await apiClient.CallAsync("messages.delete", parameters, context.BotToken, cancellationToken);

        return new DeleteMessageAction(messageId);
    }
}
