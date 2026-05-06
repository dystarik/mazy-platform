namespace MazyPlatform.Service.Bot.Manager.Api.Common.Mapping;

using DomainBotStatus = MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects.BotStatus;
using ProtoBotStatus = MazyPlatform.Contracts.Bot.Grpc.Manager.BotStatus;

internal static class BotStatusMappingExtensions
{
    internal static ProtoBotStatus ToProto(this DomainBotStatus domain) => domain switch
    {
        DomainBotStatus.Inactive => ProtoBotStatus.Inactive,
        DomainBotStatus.Active => ProtoBotStatus.Active,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null),
    };
}
