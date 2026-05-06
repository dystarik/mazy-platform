namespace MazyPlatform.Service.Notification.Messaging.Dispatchers;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

using MazyPlatform.Contracts.Core;

using Microsoft.Extensions.DependencyInjection;

internal sealed class IntegrationEventDispatcher(IServiceProvider serviceProvider) : IIntegrationEventDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly ConcurrentDictionary<string, EventHandlerPlan> Cache = new(StringComparer.Ordinal);

    public static void BuildCache()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

        var handlerTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces(), (handlerType, interfaceType) => new { handlerType, interfaceType })
            .Where(x => x.interfaceType.IsGenericType &&
                        x.interfaceType.GetGenericTypeDefinition() == handlerInterfaceType);

        foreach (var item in handlerTypes)
        {
            var eventType = item.interfaceType.GetGenericArguments()[0];

            var attribute = eventType.GetCustomAttribute<IntegrationEventTypeAttribute>()
                ?? throw new InvalidOperationException(
                    $"Событие {eventType.Name} не имеет атрибута [IntegrationEventType].");

            var routingKey = attribute.RoutingKey;

            var genericHandlerType = handlerInterfaceType.MakeGenericType(eventType);

            var handleMethod = genericHandlerType.GetMethod(
                nameof(IIntegrationEventHandler<>.HandleAsync),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: [eventType, typeof(CancellationToken)],
                modifiers: null)
                ?? throw new InvalidOperationException(
                    $"Интерфейс {genericHandlerType.FullName} не содержит метод HandleAsync.");

            Cache[routingKey] = new EventHandlerPlan(eventType, genericHandlerType, handleMethod);
        }
    }

    public async Task DispatchAsync(string routingKey, string json, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        if (!Cache.TryGetValue(routingKey, out var plan))
            return;

        var @event = JsonSerializer.Deserialize(json, plan.EventType, JsonOptions)
            ?? throw new InvalidOperationException($"Не удалось десериализовать событие. RoutingKey: {routingKey}.");

        await using var scope = serviceProvider.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetService(plan.HandlerType)
            ?? throw new InvalidOperationException($"Handler для события не зарегистрирован в DI. RoutingKey: {routingKey}.");

        var task = (Task)plan.HandleMethod.Invoke(handler, [@event, cancellationToken])!;
        await task.ConfigureAwait(false);
    }

    private sealed record EventHandlerPlan(Type EventType, Type HandlerType, MethodInfo HandleMethod);
}
