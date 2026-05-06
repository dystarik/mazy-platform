namespace MazyPlatform.Scenario.Vk.Events;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Парсер входящих событий VK Callback API.
/// </summary>
public static class VkEventParser
{
    /// <summary>
    /// Парсит JSON от VK Callback API в <see cref="VkIncomingEvent"/>.
    /// </summary>
    /// <param name="json">JSON тела webhook.</param>
    /// <param name="botId">Идентификатор бота (маппинг group_id → Guid делает вызывающий код).</param>
    /// <returns>Событие VK.</returns>
    public static VkIncomingEvent Parse(string json, Guid botId)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var message = root.GetProperty("object").GetProperty("message");

        var peerId = message.GetProperty("peer_id").GetInt64();
        var fromId = message.GetProperty("from_id").GetInt64();
        var text = message.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
        var messageId = message.TryGetProperty("id", out var idProp)
            ? idProp.GetInt64().ToString(CultureInfo.InvariantCulture)
            : null;

        var (eventType, payload, imageUrl, imageCaption) = DetermineEventDetails(message);

        return new VkIncomingEvent
        {
            EventType = eventType,
            BotId = botId,
            PlatformUserId = fromId.ToString(CultureInfo.InvariantCulture),
            ChatId = peerId.ToString(CultureInfo.InvariantCulture),
            Text = text,
            Payload = payload,
            ImageUrl = imageUrl,
            ImageCaption = imageCaption,
            MessageId = messageId,
            RawJson = json,
        };
    }

    private static (
        IncomingEventType EventType,
        string? Payload,
        string? ImageUrl,
        string? ImageCaption) DetermineEventDetails(JsonElement message)
    {
        if (message.TryGetProperty("payload", out var payloadProp))
        {
            var payload = payloadProp.ValueKind == JsonValueKind.String
                ? payloadProp.GetString()
                : payloadProp.GetRawText();

            return (IncomingEventType.ButtonPress, NormalizePayload(payload), null, null);
        }

        if (TryExtractPhoto(message, out var imageUrl))
        {
            var caption = message.TryGetProperty("text", out var captionProp)
                && !string.IsNullOrEmpty(captionProp.GetString())
                ? captionProp.GetString()
                : null;

            return (IncomingEventType.Image, null, imageUrl, caption);
        }

        return (IncomingEventType.Message, null, null, null);
    }

    private static string? NormalizePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return payload;

        try
        {
            using var document = JsonDocument.Parse(payload);

            if (document.RootElement.TryGetProperty("p", out var payloadValue))
                return payloadValue.GetString() ?? payload;
        }
        catch (JsonException)
        {
        }

        return payload;
    }

    private static bool TryExtractPhoto(JsonElement message, out string? imageUrl)
    {
        imageUrl = null;

        if (!message.TryGetProperty("attachments", out var attachments)
            || attachments.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var attachment in attachments.EnumerateArray())
        {
            if (!attachment.TryGetProperty("type", out var typeProp)
                || !string.Equals(typeProp.GetString(), "photo", StringComparison.Ordinal))
            {
                continue;
            }

            imageUrl = GetLargestPhotoUrl(attachment.GetProperty("photo").GetProperty("sizes"));
            return true;
        }

        return false;
    }

    private static string? GetLargestPhotoUrl(JsonElement sizes)
    {
        string? url = null;
        var maxArea = 0;

        foreach (var size in sizes.EnumerateArray())
        {
            var width = size.GetProperty("width").GetInt32();
            var height = size.GetProperty("height").GetInt32();
            var area = width * height;

            if (area > maxArea)
            {
                maxArea = area;
                url = size.GetProperty("url").GetString();
            }
        }

        return url;
    }
}
