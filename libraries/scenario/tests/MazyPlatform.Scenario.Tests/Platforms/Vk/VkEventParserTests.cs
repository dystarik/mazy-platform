namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Vk.Events;

public class VkEventParserTests
{
    [Test]
    public async Task Parse_MessageEventPayload_ReturnsButtonPress()
    {
        var botId = Guid.NewGuid();
        var json = """
                   {
                     "type": "message_event",
                     "object": {
                       "peer_id": 2000000001,
                       "user_id": 123,
                       "conversation_message_id": 42,
                       "payload": "about"
                     }
                   }
                   """;

        var result = VkEventParser.Parse(json, botId);

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.ButtonPress);
        await Assert.That(result.Payload).IsEqualTo("about");
        await Assert.That(result.ChatId).IsEqualTo("2000000001");
        await Assert.That(result.PlatformUserId).IsEqualTo("123");
        await Assert.That(result.MessageId).IsEqualTo("42");
        await Assert.That(result.RawJson).IsEqualTo(json);
    }

    [Test]
    public async Task Parse_MessageNewPayload_IsRegularMessage()
    {
        var botId = Guid.NewGuid();
        var json = """
                   {
                     "object": {
                       "message": {
                         "peer_id": 2000000001,
                         "from_id": 123,
                         "text": "О нас",
                         "payload": "about"
                       }
                     }
                   }
                   """;

        var result = VkEventParser.Parse(json, botId);

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.Message);
        await Assert.That(result.Payload).IsNull();
        await Assert.That(result.Text).IsEqualTo("О нас");
    }
}
