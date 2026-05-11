namespace MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

using MazyPlatform.Contracts.Bot.Grpc.Manager;

internal static class BotIntegrationTestData
{
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

    public static ActiveBotInfo MissingCredentials()
    {
        var bot = TelegramActiveBot();
        bot.Credentials = null;
        return bot;
    }

    public static ActiveBotInfo VkWithNonNumericCommunityId()
    {
        return VkActiveBot(communityId: "not-a-number");
    }
}
