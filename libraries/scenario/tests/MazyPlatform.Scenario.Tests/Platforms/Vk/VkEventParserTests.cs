namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Vk.Events;

public class VkEventParserTests
{
    [Test]
    public async Task Parse_ButtonPayloadJson_NormalizesPayloadValue()
    {
        var botId = Guid.NewGuid();
        var json = """
                   {
                     "object": {
                       "message": {
                         "peer_id": 2000000001,
                         "from_id": 123,
                         "text": "О нас",
                         "payload": "{\"p\":\"about\"}"
                       }
                     }
                   }
                   """;

        var result = VkEventParser.Parse(json, botId);

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.ButtonPress);
        await Assert.That(result.Payload).IsEqualTo("about");
        await Assert.That(result.RawJson).IsEqualTo(json);
    }

    [Test]
    public async Task Parse_PlainButtonPayload_KeepsOriginalValue()
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

        await Assert.That(result.EventType).IsEqualTo(IncomingEventType.ButtonPress);
        await Assert.That(result.Payload).IsEqualTo("about");
    }
}
