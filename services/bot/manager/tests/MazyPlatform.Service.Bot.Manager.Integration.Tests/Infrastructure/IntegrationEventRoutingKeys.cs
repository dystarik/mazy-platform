namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using System.Reflection;

using MazyPlatform.Contracts.Core;

internal static class IntegrationEventRoutingKeys
{
    public static string Of<TEvent>()
    {
        var attribute = typeof(TEvent).GetCustomAttribute<IntegrationEventTypeAttribute>();
        return attribute?.RoutingKey
            ?? throw new InvalidOperationException($"Event {typeof(TEvent).Name} has no IntegrationEventTypeAttribute.");
    }
}
