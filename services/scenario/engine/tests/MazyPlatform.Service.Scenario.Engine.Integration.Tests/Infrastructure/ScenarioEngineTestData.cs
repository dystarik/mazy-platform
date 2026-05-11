namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using System.Globalization;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Grpc.Manager;

internal static class ScenarioEngineTestData
{
    public static ActiveBotInfo MalformedBotId()
    {
        var bot = TelegramActiveBot();
        bot.BotInstanceId = "not-a-guid";
        return bot;
    }

    public static ActiveBotInfo MalformedProjectId()
    {
        var bot = TelegramActiveBot();
        bot.ProjectId = "not-a-guid";
        return bot;
    }

    public static string MinimalReceiveMessageGraphJson(Guid? startNodeId = null)
    {
        var nodeId = startNodeId ?? Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{nodeId}}",
              "nodes": [
                {
                  "id": "{{nodeId}}",
                  "type": "receive_message",
                  "params": {
                    "messageTextVariable": "message_text",
                    "messageIdVariable": "message_id"
                  }
                }
              ],
              "connections": []
            }
            """;
    }

    public static string DelayThenSetVariableGraphJson(Guid? delayNodeId = null)
    {
        var delayId = delayNodeId ?? Guid.NewGuid();
        var setId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{delayId}}",
              "nodes": [
                {
                  "id": "{{delayId}}",
                  "type": "delay",
                  "params": { "seconds": 1 }
                },
                {
                  "id": "{{setId}}",
                  "type": "set_variable",
                  "params": { "variable": "resumed", "value": "yes" }
                }
              ],
              "connections": [
                { "from": "{{delayId}}", "to": "{{setId}}" }
              ]
            }
            """;
    }

    public static string EntryPointGraphJson(Guid? waitingNodeId = null, string payload = "menu")
    {
        var startId = waitingNodeId ?? Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{startId}}",
              "nodes": [
                {
                  "id": "{{startId}}",
                  "type": "receive_message",
                  "params": { "messageTextVariable": "message_text" }
                },
                {
                  "id": "{{entryId}}",
                  "type": "receive_button_press",
                  "params": {
                    "buttonPayloadVariable": "payload",
                    "expectedPayloads": ["{{payload}}"],
                    "isEntry": true
                  }
                },
                {
                  "id": "{{setId}}",
                  "type": "set_variable",
                  "params": { "variable": "entry_route", "value": "hit" }
                }
              ],
              "connections": [
                { "from": "{{entryId}}", "to": "{{setId}}" }
              ]
            }
            """;
    }

    public static string GraphWithoutNodesJson(Guid? startNodeId = null)
    {
        var nodeId = startNodeId ?? Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{nodeId}}",
              "connections": []
            }
            """;
    }

    public static string LoopGraphJson()
    {
        var nodeId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{nodeId}}",
              "nodes": [
                {
                  "id": "{{nodeId}}",
                  "type": "set_variable",
                  "params": { "variable": "loop", "value": "again" }
                }
              ],
              "connections": [
                { "from": "{{nodeId}}", "to": "{{nodeId}}" }
              ]
            }
            """;
    }

    public static ActiveBotInfo MissingCredentials()
    {
        var bot = TelegramActiveBot();
        bot.Credentials = null;
        return bot;
    }

    public static BotCredentials TelegramCredentials(string? accessToken = null)
    {
        return new BotCredentials
        {
            PlatformType = BotPlatformType.Telegram,
            Telegram = new TelegramCredentials
            {
                AccessToken = accessToken ?? $"tg-{Guid.NewGuid():N}",
            },
        };
    }

    public static ActiveBotInfo TelegramActiveBot(
        Guid? botInstanceId = null,
        Guid? projectId = null,
        int scenarioVersion = 1,
        string? accessToken = null)
    {
        return new ActiveBotInfo
        {
            BotInstanceId = (botInstanceId ?? Guid.NewGuid()).ToString(),
            ProjectId = (projectId ?? Guid.NewGuid()).ToString(),
            ScenarioVersion = scenarioVersion,
            Credentials = TelegramCredentials(accessToken),
        };
    }

    public static string TelegramMessagePayload(string text = "hello", long platformUserId = 1001, long chatId = 2002, long messageId = 3003)
    {
        return $$"""
            {
              "update_id": 1,
              "message": {
                "message_id": {{messageId.ToString(CultureInfo.InvariantCulture)}},
                "from": { "id": {{platformUserId.ToString(CultureInfo.InvariantCulture)}} },
                "chat": { "id": {{chatId.ToString(CultureInfo.InvariantCulture)}} },
                "text": "{{text}}"
              }
            }
            """;
    }

    public static string ReceiveMessageThenSetVariableGraphJson(Guid? receiveNodeId = null)
    {
        var receiveId = receiveNodeId ?? Guid.NewGuid();
        var setId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{receiveId}}",
              "nodes": [
                {
                  "id": "{{receiveId}}",
                  "type": "receive_message",
                  "params": {
                    "messageTextVariable": "message_text",
                    "messageIdVariable": "message_id"
                  }
                },
                {
                  "id": "{{setId}}",
                  "type": "set_variable",
                  "params": { "variable": "after_receive", "value": "done" }
                }
              ],
              "connections": [
                { "from": "{{receiveId}}", "to": "{{setId}}" }
              ]
            }
            """;
    }

    public static string SetVariableThenReceiveMessageGraphJson(Guid? setNodeId = null)
    {
        var setId = setNodeId ?? Guid.NewGuid();
        var receiveId = Guid.NewGuid();
        return $$"""
            {
              "startNodeId": "{{setId}}",
              "nodes": [
                {
                  "id": "{{setId}}",
                  "type": "set_variable",
                  "params": { "variable": "before_wait", "value": "saved" }
                },
                {
                  "id": "{{receiveId}}",
                  "type": "receive_message",
                  "params": { "messageTextVariable": "message_text" }
                }
              ],
              "connections": [
                { "from": "{{setId}}", "to": "{{receiveId}}" }
              ]
            }
            """;
    }

    public static string TelegramCallbackPayload(string payload = "menu", long platformUserId = 1001, long chatId = 2002, long messageId = 3003)
    {
        return $$"""
            {
              "update_id": 1,
              "callback_query": {
                "id": "callback-{{Guid.NewGuid():N}}",
                "from": { "id": {{platformUserId.ToString(CultureInfo.InvariantCulture)}} },
                "message": {
                  "message_id": {{messageId.ToString(CultureInfo.InvariantCulture)}},
                  "chat": { "id": {{chatId.ToString(CultureInfo.InvariantCulture)}} }
                },
                "data": "{{payload}}"
              }
            }
            """;
    }

    public static BotCredentials VkCredentials(string? accessToken = null, string communityId = "12345")
    {
        return new BotCredentials
        {
            PlatformType = BotPlatformType.Vk,
            Vk = new VkCredentials
            {
                AccessToken = accessToken ?? $"vk-{Guid.NewGuid():N}",
                CommunityId = communityId,
            },
        };
    }

    public static ActiveBotInfo VkActiveBot(
        Guid? botInstanceId = null,
        Guid? projectId = null,
        int scenarioVersion = 1,
        string? accessToken = null,
        string communityId = "12345")
    {
        return new ActiveBotInfo
        {
            BotInstanceId = (botInstanceId ?? Guid.NewGuid()).ToString(),
            ProjectId = (projectId ?? Guid.NewGuid()).ToString(),
            ScenarioVersion = scenarioVersion,
            Credentials = VkCredentials(accessToken, communityId),
        };
    }

    public static string VkMessagePayload(string text = "hello", long platformUserId = 1001, long peerId = 2002, long messageId = 3003)
    {
        return $$"""
            {
              "type": "message_new",
              "object": {
                "message": {
                  "id": {{messageId.ToString(CultureInfo.InvariantCulture)}},
                  "peer_id": {{peerId.ToString(CultureInfo.InvariantCulture)}},
                  "from_id": {{platformUserId.ToString(CultureInfo.InvariantCulture)}},
                  "text": "{{text}}"
                }
              }
            }
            """;
    }

    public static string VkMessageEventPayload(string payload = "menu", long platformUserId = 1001, long peerId = 2002, long messageId = 3003)
    {
        return $$"""
            {
              "type": "message_event",
              "object": {
                "peer_id": {{peerId.ToString(CultureInfo.InvariantCulture)}},
                "user_id": {{platformUserId.ToString(CultureInfo.InvariantCulture)}},
                "conversation_message_id": {{messageId.ToString(CultureInfo.InvariantCulture)}},
                "payload": "\"{{payload}}\""
              }
            }
            """;
    }

    public static ActiveBotInfo VkWithNonNumericCommunityId()
    {
        return VkActiveBot(communityId: "not-a-number");
    }
}
