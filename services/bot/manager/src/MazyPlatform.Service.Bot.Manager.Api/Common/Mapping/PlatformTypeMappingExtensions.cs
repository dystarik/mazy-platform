namespace MazyPlatform.Service.Bot.Manager.Api.Common.Mapping;

using DomainPlatformType = MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects.PlatformType;
using ProtoPlatformType = MazyPlatform.Contracts.Bot.Grpc.Manager.BotPlatformType;

internal static class PlatformTypeMappingExtensions
{
    internal static DomainPlatformType ToDomain(this ProtoPlatformType proto) => proto switch
    {
        ProtoPlatformType.Vk => DomainPlatformType.Vk,
        ProtoPlatformType.Telegram => DomainPlatformType.Telegram,
        _ => throw new ArgumentOutOfRangeException(nameof(proto), proto, null),
    };

    internal static ProtoPlatformType ToProto(this DomainPlatformType domain) => domain switch
    {
        DomainPlatformType.Vk => ProtoPlatformType.Vk,
        DomainPlatformType.Telegram => ProtoPlatformType.Telegram,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null),
    };
}
