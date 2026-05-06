namespace MazyPlatform.Service.Bot.Manager.Api.Common.Mapping;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

internal static class BotCredentialsMappingExtensions
{
    internal static BotCredentials ToProto(this IBotCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        return credentials switch
        {
            VkBotCredentials vk => new BotCredentials
            {
                PlatformType = vk.PlatformType.ToProto(),
                Vk = new VkCredentials
                {
                    AccessToken = vk.AccessToken,
                    CommunityId = vk.CommunityId,
                },
            },
            TelegramBotCredentials telegram => new BotCredentials
            {
                PlatformType = telegram.PlatformType.ToProto(),
                Telegram = new TelegramCredentials
                {
                    AccessToken = telegram.AccessToken,
                },
            },
            _ => throw new InvalidOperationException($"Неизвестный тип credentials: {credentials.GetType()}"),
        };
    }
}
