namespace MazyPlatform.Scenario.Telegram.Events;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Парсер входящих событий Telegram webhook.
/// </summary>
public static class TelegramEventParser
{
    /// <summary>
    /// Парсит JSON Telegram update в <see cref="TelegramIncomingEvent"/>.
    /// </summary>
    /// <param name="json">JSON тела webhook.</param>
    /// <param name="botId">Идентификатор бота.</param>
    /// <returns>Событие Telegram.</returns>
    public static TelegramIncomingEvent Parse(string json, Guid botId)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("callback_query", out var callbackQuery))
        {
            return ParseCallbackQuery(callbackQuery, json, botId);
        }

        if (root.TryGetProperty("message", out var message))
        {
            return ParseMessage(message, json, botId);
        }

        throw new InvalidOperationException("Telegram update не содержит поддерживаемого события.");
    }

    private static TelegramIncomingEvent ParseCallbackQuery(JsonElement callbackQuery, string json, Guid botId)
    {
        if (!callbackQuery.TryGetProperty("message", out var message))
        {
            throw new InvalidOperationException("Telegram callback_query без message не поддерживается.");
        }

        var from = callbackQuery.GetProperty("from");
        var chat = message.GetProperty("chat");
        var callbackQueryId = callbackQuery.GetProperty("id").GetString();
        var messageId = message.GetProperty("message_id").GetInt64();
        var data = callbackQuery.TryGetProperty("data", out var dataElement)
            ? dataElement.GetString()
            : null;

        return new TelegramIncomingEvent
        {
            EventType = IncomingEventType.ButtonPress,
            BotId = botId,
            PlatformUserId = GetInt64String(from, "id"),
            ChatId = GetInt64String(chat, "id"),
            Payload = data,
            MessageId = messageId.ToString(CultureInfo.InvariantCulture),
            CallbackQueryId = callbackQueryId,
            RawJson = json,
        };
    }

    private static TelegramIncomingEvent ParseMessage(JsonElement message, string json, Guid botId)
    {
        var chat = message.GetProperty("chat");
        var from = message.TryGetProperty("from", out var fromElement)
            ? fromElement
            : chat;
        var messageId = message.GetProperty("message_id").GetInt64().ToString(CultureInfo.InvariantCulture);
        var platformUserId = GetInt64String(from, "id");
        var chatId = GetInt64String(chat, "id");

        if (TryGetPhotoFileId(message, out var fileId))
        {
            var caption = message.TryGetProperty("caption", out var captionElement)
                ? captionElement.GetString()
                : null;

            return new TelegramIncomingEvent
            {
                EventType = IncomingEventType.Image,
                BotId = botId,
                PlatformUserId = platformUserId,
                ChatId = chatId,
                ImageUrl = fileId,
                ImageCaption = caption,
                MessageId = messageId,
                RawJson = json,
            };
        }

        var text = message.TryGetProperty("text", out var textElement)
            ? textElement.GetString()
            : null;

        return new TelegramIncomingEvent
        {
            EventType = IncomingEventType.Message,
            BotId = botId,
            PlatformUserId = platformUserId,
            ChatId = chatId,
            Text = text,
            MessageId = messageId,
            RawJson = json,
        };
    }

    private static bool TryGetPhotoFileId(JsonElement message, out string? fileId)
    {
        fileId = null;

        if (!message.TryGetProperty("photo", out var photos)
            || photos.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var maxArea = -1;
        foreach (var photo in photos.EnumerateArray())
        {
            var width = photo.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : 0;
            var height = photo.TryGetProperty("height", out var heightElement) ? heightElement.GetInt32() : 0;
            var area = width * height;

            if (area <= maxArea)
            {
                continue;
            }

            maxArea = area;
            fileId = photo.GetProperty("file_id").GetString();
        }

        return fileId is not null;
    }

    private static string GetInt64String(JsonElement parent, string propertyName) =>
        parent.GetProperty(propertyName).GetInt64().ToString(CultureInfo.InvariantCulture);
}
