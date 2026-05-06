namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging.RabbitMq;

using System.Text;
using System.Text.Json;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Observability;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

internal sealed partial class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqIntegrationEventPublisher> logger,
    RabbitMqConnectionFactory connectionFactory) : IIntegrationEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IChannel? _channel;

    public void Dispose() => _channel?.Dispose();

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await EnsureChannelAsync(cancellationToken);

        var routingKey = (typeof(TEvent)
            .GetCustomAttributes(typeof(IntegrationEventTypeAttribute), false)
            .FirstOrDefault() as IntegrationEventTypeAttribute)?.RoutingKey
            ?? throw new InvalidOperationException(
                $"Событие {typeof(TEvent).FullName} не содержит атрибут [IntegrationEventType].");

        var json = JsonSerializer.Serialize(integrationEvent, typeof(TEvent));
        var body = Encoding.UTF8.GetBytes(json);
        var traceId = TraceContext.GetOrCreate();

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { TraceContext.HeaderName, Encoding.UTF8.GetBytes(traceId) },
            },
        };

        using (logger.BeginScope(new Dictionary<string, object>(StringComparer.Ordinal) { ["TraceId"] = traceId }))
        {
            await _channel!.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            EventPublished(routingKey);
        }
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return;

        var connection = await connectionFactory.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано событие с routing key: {RoutingKey}.")]
    private partial void EventPublished(string routingKey);
    #endregion
}
