namespace MazyPlatform.Service.Bot.Integration.Messaging;

using System.Text;
using System.Text.Json;

using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Service.Bot.Integration.Configuration.Options;
using MazyPlatform.Service.Bot.Integration.Observability;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using Serilog.Context;

internal sealed partial class RabbitMqEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private const string RoutingKey = "bot.integration.incoming_event";

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(BotIncomingEventIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var traceId = TraceContext.GetOrCreate();

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Headers = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { TraceContext.HeaderName, Encoding.UTF8.GetBytes(traceId) },
            },
        };

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _channel!.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            LogPublished(@event.BotId, @event.Platform.ToString());
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();

        if (_connection is not null)
            await _connection.CloseAsync();

        _initLock.Dispose();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            return;

        await _initLock.WaitAsync(cancellationToken);

        try
        {
            if (_channel is not null)
                return;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
            };

            LogConnecting(_options.Host, _options.Port);

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: _options.ExchangeType,
                durable: true,
                cancellationToken: cancellationToken);

            LogConnected();
        }
        finally
        {
            _initLock.Release();
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Подключение к RabbitMQ. Host: {Host}, Port: {Port}.")]
    private partial void LogConnecting(string host, int port);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Подключение к RabbitMQ установлено.")]
    private partial void LogConnected();

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Событие опубликовано. BotId: {BotId}, Platform: {Platform}.")]
    private partial void LogPublished(Guid botId, string platform);
}
