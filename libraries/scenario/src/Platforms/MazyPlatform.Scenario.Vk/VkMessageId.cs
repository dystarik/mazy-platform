namespace MazyPlatform.Scenario.Vk;

using System.Globalization;
using System.Text.Json;

internal static class VkMessageId
{
    private const string _messageIdPrefix = "vk:mid:";
    private const string _conversationMessageIdPrefix = "vk:cmid:";

    public static string FromMessageId(long messageId) =>
        FromMessageId(messageId.ToString(CultureInfo.InvariantCulture));

    public static string FromMessageId(string messageId) => $"{_messageIdPrefix}{messageId}";

    public static string FromConversationMessageId(long conversationMessageId) =>
        FromConversationMessageId(conversationMessageId.ToString(CultureInfo.InvariantCulture));

    public static string FromConversationMessageId(string conversationMessageId) =>
        $"{_conversationMessageIdPrefix}{conversationMessageId}";

    public static string? FromSendResponse(JsonElement response) =>
        response.ValueKind == JsonValueKind.Number
            ? FromMessageId(response.GetInt64())
            : null;

    public static bool TryGetMessageId(string messageId, out string value)
    {
        if (messageId.StartsWith(_messageIdPrefix, StringComparison.Ordinal))
        {
            value = messageId[_messageIdPrefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static bool TryGetConversationMessageId(string messageId, out string value)
    {
        if (messageId.StartsWith(_conversationMessageIdPrefix, StringComparison.Ordinal))
        {
            value = messageId[_conversationMessageIdPrefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }
}
