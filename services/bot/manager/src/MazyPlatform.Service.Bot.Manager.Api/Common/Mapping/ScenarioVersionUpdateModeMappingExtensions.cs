namespace MazyPlatform.Service.Bot.Manager.Api.Common.Mapping;

using DomainScenarioVersionUpdateMode = MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects.ScenarioVersionUpdateMode;
using ProtoScenarioVersionUpdateMode = MazyPlatform.Contracts.Bot.Grpc.Manager.BotScenarioVersionUpdateMode;

internal static class ScenarioVersionUpdateModeMappingExtensions
{
    internal static DomainScenarioVersionUpdateMode ToDomainOrDefault(this ProtoScenarioVersionUpdateMode proto) => proto switch
    {
        ProtoScenarioVersionUpdateMode.Manual => DomainScenarioVersionUpdateMode.Manual,
        ProtoScenarioVersionUpdateMode.Auto or ProtoScenarioVersionUpdateMode.Unspecified => DomainScenarioVersionUpdateMode.Auto,
        _ => throw new ArgumentOutOfRangeException(nameof(proto), proto, null),
    };

    internal static ProtoScenarioVersionUpdateMode ToProto(this DomainScenarioVersionUpdateMode domain) => domain switch
    {
        DomainScenarioVersionUpdateMode.Auto => ProtoScenarioVersionUpdateMode.Auto,
        DomainScenarioVersionUpdateMode.Manual => ProtoScenarioVersionUpdateMode.Manual,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null),
    };
}
