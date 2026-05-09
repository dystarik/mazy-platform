namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk;
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
        if (VkMessageId.TryGetMessageId(messageId, out var vkMessageId))
        {
            await DeleteByMessageIdAsync(context, vkMessageId, cancellationToken);
        }
        else if (VkMessageId.TryGetConversationMessageId(messageId, out var conversationMessageId))
        {
            await DeleteByConversationMessageIdAsync(context, conversationMessageId, cancellationToken);
        }
        else
        {
            var response = await DeleteByMessageIdAsync(context, messageId, cancellationToken);
            if (!IsDeleteSuccessful(response, messageId))
            {
                await DeleteByConversationMessageIdAsync(context, messageId, cancellationToken);
            }
        }

        return new DeleteMessageAction(messageId);
    }

    private static bool IsDeleteSuccessful(JsonElement response, string messageId)
    {
        return response.ValueKind switch
        {
            JsonValueKind.Number => response.GetInt32() == 1,
            JsonValueKind.True => true,
            JsonValueKind.Object => IsObjectDeleteSuccessful(response, messageId),
            JsonValueKind.Array => response.EnumerateArray().Any(IsDeleteSuccessfulValue),
            _ => false,
        };
    }

    private static bool IsObjectDeleteSuccessful(JsonElement response, string messageId)
    {
        if (response.TryGetProperty(messageId, out var result))
        {
            return IsDeleteSuccessfulValue(result);
        }

        return response.EnumerateObject().Any(property => IsDeleteSuccessfulValue(property.Value));
    }

    private static bool IsDeleteSuccessfulValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetInt32() == 1,
            JsonValueKind.True => true,
            JsonValueKind.String => string.Equals(value.GetString(), "1", StringComparison.Ordinal)
                || string.Equals(value.GetString(), bool.TrueString, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private async Task<JsonElement> DeleteByMessageIdAsync(
        ExecutionContext context,
        string messageId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["message_ids"] = messageId,
            ["delete_for_all"] = "1",
        };

        return await apiClient.CallAsync("messages.delete", parameters, context.BotToken, cancellationToken);
    }

    private async Task<JsonElement> DeleteByConversationMessageIdAsync(
        ExecutionContext context,
        string conversationMessageId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["cmids"] = conversationMessageId,
            ["delete_for_all"] = "1",
            ["peer_id"] = context.IncomingEvent!.ChatId,
        };

        return await apiClient.CallAsync("messages.delete", parameters, context.BotToken, cancellationToken);
    }
}
