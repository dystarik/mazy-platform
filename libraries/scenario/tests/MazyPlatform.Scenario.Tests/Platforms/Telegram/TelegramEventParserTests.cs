namespace MazyPlatform.Scenario.Tests.Platforms.Telegram;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Telegram.Events;

public class TelegramEventParserTests
{
    [Test]
    public async Task Parse_TextMessage_ReturnsMessageEvent()
    {
        var botId = Guid.NewGuid();
        var json = """
                   {
                     "message": {
                       "message_id": 10,
                       "from": { "id": 123 },
                       "chat": { "id": 456 },
                       "text": "Привет"
                     }
                   }
                   """;

        var result = TelegramEventParser.Parse(json, botId);

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.Message);
        await Assert.That(result.PlatformUserId).IsEqualTo("123");
        await Assert.That(result.ChatId).IsEqualTo("456");
        await Assert.That(result.Text).IsEqualTo("Привет");
        await Assert.That(result.MessageId).IsEqualTo("10");
    }

    [Test]
    public async Task Parse_CallbackQuery_ReturnsButtonPressEvent()
    {
        var botId = Guid.NewGuid();
        var json = """
                   {
                     "callback_query": {
                       "id": "callback-1",
                       "from": { "id": 123 },
                       "data": "menu",
                       "message": {
                         "message_id": 10,
                         "chat": { "id": 456 }
                       }
                     }
                   }
                   """;

        var result = TelegramEventParser.Parse(json, botId);

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.ButtonPress);
        await Assert.That(result.Payload).IsEqualTo("menu");
        await Assert.That(result.CallbackQueryId).IsEqualTo("callback-1");
        await Assert.That(result.ChatId).IsEqualTo("456");
    }

    [Test]
    public async Task Parse_PhotoMessage_ReturnsLargestFileId()
    {
        var botId = Guid.NewGuid();
        var json = """
                   {
                     "message": {
                       "message_id": 10,
                       "from": { "id": 123 },
                       "chat": { "id": 456 },
                       "caption": "Фото",
                       "photo": [
                         { "file_id": "small", "width": 90, "height": 90 },
                         { "file_id": "large", "width": 1024, "height": 768 }
                       ]
                     }
                   }
                   """;

        var result = TelegramEventParser.Parse(json, botId);

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.Image);
        await Assert.That(result.ImageUrl).IsEqualTo("large");
        await Assert.That(result.ImageCaption).IsEqualTo("Фото");
    }
}
