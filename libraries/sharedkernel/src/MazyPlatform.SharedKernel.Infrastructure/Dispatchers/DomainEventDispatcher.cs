namespace MazyPlatform.SharedKernel.Infrastructure.Dispatchers;

using System.Collections.Concurrent;
using System.Reflection;

using MazyPlatform.SharedKernel.Application.Abstractions.Events;
using MazyPlatform.SharedKernel.Domain.Abstractions;

using Microsoft.Extensions.DependencyInjection;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvokePlan> Cache = new();

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        if (domainEvents.Count == 0)
            return;

        using var scope = serviceProvider.CreateScope();

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is null)
                throw new ArgumentException("Коллекция доменных событий содержит null", nameof(domainEvents));

            var eventType = domainEvent.GetType();
            var plan = Cache.GetOrAdd(eventType, CreateHandlerPlan);

            var handlers = scope.ServiceProvider.GetServices(plan.HandlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                    continue;

                var task = (Task)plan.HandleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                await task.ConfigureAwait(false);
            }
        }
    }

    private static HandlerInvokePlan CreateHandlerPlan(Type eventType)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        var handleMethod = handlerType.GetMethod(
            "HandleAsync",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [eventType, typeof(CancellationToken)],
            modifiers: null)
            ?? throw new InvalidOperationException(
                $"Интерфейс {handlerType.FullName} не содержит метод HandleAsync({eventType.Name}, CancellationToken)");

        return new HandlerInvokePlan(handlerType, handleMethod);
    }

    private sealed record HandlerInvokePlan(Type HandlerType, MethodInfo HandleMethod);
}
