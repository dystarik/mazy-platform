namespace MazyPlatform.Service.Scenario.Engine.Messaging;

using System.Text;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Scenario.Engine.Configuration.Options.RabbitMq;
using MazyPlatform.Service.Scenario.Engine.Observability;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

internal sealed partial class RabbitMqConsumerService(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerService> logger,
    IIntegrationEventDispatcher dispatcher) : BackgroundService
{
    private readonly List<IChannel> _channels = [];
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        LogStopping();

        foreach (var channel in _channels)
            await channel.CloseAsync(cancellationToken);

        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _connection = await CreateConnectionAsync(cancellationToken);
        await InitializeQueuesAsync(cancellationToken);
        LogStarted();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
        };

        LogConnecting(_options.Host, _options.Port);
        return await factory.CreateConnectionAsync(cancellationToken);
    }

    private async Task InitializeQueuesAsync(CancellationToken cancellationToken)
    {
        foreach (var queueOptions in _options.Queues)
        {
            var channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: _options.DeadLetterExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queueOptions.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queueOptions.DeadLetterQueueName,
                exchange: _options.DeadLetterExchangeName,
                routingKey: queueOptions.DeadLetterQueueName,
                cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: _options.ExchangeType,
                durable: true,
                cancellationToken: cancellationToken);

            var queueArguments = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { "x-dead-letter-exchange", _options.DeadLetterExchangeName },
                { "x-dead-letter-routing-key", queueOptions.DeadLetterQueueName },
            };

            await channel.QueueDeclareAsync(
                queue: queueOptions.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queueOptions.QueueName,
                exchange: _options.ExchangeName,
                routingKey: queueOptions.RoutingKey,
                cancellationToken: cancellationToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _options.Prefetch,
                global: false,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea, cancellationToken);

            await channel.BasicConsumeAsync(
                queue: queueOptions.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _channels.Add(channel);
            LogListeningQueue(queueOptions.QueueName, queueOptions.RoutingKey);
        }
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var routingKey = ea.RoutingKey;
        var json = Encoding.UTF8.GetString(ea.Body.Span);
        var traceId = GetOrCreateTraceId(ea);

        using (TraceContext.BeginScope(traceId))
        using (LogContext.PushProperty("TraceId", traceId))
        {
            LogMessageReceived(routingKey);

            try
            {
                await dispatcher.DispatchAsync(routingKey, json, cancellationToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                LogMessageError(ex, routingKey, ex.Message);
                var shouldRequeue = !ea.Redelivered;
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: shouldRequeue, cancellationToken: cancellationToken);
            }
        }
    }

    private string GetOrCreateTraceId(BasicDeliverEventArgs ea)
    {
        if (ea.BasicProperties.Headers?.TryGetValue(TraceContext.HeaderName, out var value) == true)
        {
            var traceId = value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string text => text,
                _ => value?.ToString(),
            };

            if (!string.IsNullOrWhiteSpace(traceId))
                return traceId;
        }

        return TraceContext.GetOrCreate();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Остановка RabbitMQ consumer.")]
    private partial void LogStopping();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Подключение к RabbitMQ. Host: {Host}, Port: {Port}.")]
    private partial void LogConnecting(string host, int port);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "RabbitMQ consumer запущен.")]
    private partial void LogStarted();

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Подписка на очередь. Queue: {QueueName}, RoutingKey: {RoutingKey}.")]
    private partial void LogListeningQueue(string queueName, string routingKey);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Сообщение получено. RoutingKey: {RoutingKey}.")]
    private partial void LogMessageReceived(string routingKey);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Ошибка обработки сообщения. RoutingKey: {RoutingKey}. Error: {ErrorMessage}.")]
    private partial void LogMessageError(Exception exception, string routingKey, string errorMessage);
}
