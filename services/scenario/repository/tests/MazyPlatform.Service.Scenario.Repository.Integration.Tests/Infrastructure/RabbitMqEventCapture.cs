namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using MazyPlatform.Contracts.Scenario.Repository.Events;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public sealed class RabbitMqEventCapture : IAsyncDisposable
{
    public static readonly string ProjectDeleted = IntegrationEventRoutingKeys.Of<ProjectDeletedIntegrationEvent>();
    public static readonly string ReleaseChanged = IntegrationEventRoutingKeys.Of<ScenarioReleaseChangedIntegrationEvent>();
    public static readonly string ReleaseRemoved = IntegrationEventRoutingKeys.Of<ScenarioReleaseRemovedIntegrationEvent>();
    public static readonly string VersionDeleted = IntegrationEventRoutingKeys.Of<ScenarioVersionDeletedIntegrationEvent>();

    private readonly ConcurrentQueue<CapturedScenarioEvent> _events = new();
    private readonly SemaphoreSlim _eventArrived = new(0);
    private readonly string _exchangeName;
    private readonly string _host;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;
    private IChannel? _channel;
    private IConnection? _connection;
    private string? _queueName;

    public RabbitMqEventCapture(string host, int port, string username, string password, string exchangeName)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _exchangeName = exchangeName;
    }

    public int Count => _events.Count;

    public async Task StartAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _host,
            Port = _port,
            UserName = _username,
            Password = _password,
            VirtualHost = "/",
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true);

        var queue = await _channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true);

        _queueName = queue.QueueName;
        await _channel.QueueBindAsync(_queueName, _exchangeName, ProjectDeleted);
        await _channel.QueueBindAsync(_queueName, _exchangeName, ReleaseChanged);
        await _channel.QueueBindAsync(_queueName, _exchangeName, ReleaseRemoved);
        await _channel.QueueBindAsync(_queueName, _exchangeName, VersionDeleted);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;
        await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer);
    }

    public async Task<CapturedScenarioEvent> WaitForProjectAsync(
        string routingKey,
        Guid projectId,
        int afterCount,
        CancellationToken cancellationToken = default)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var match = _events
                .ToArray()
                .Skip(afterCount)
                .FirstOrDefault(e => string.Equals(e.RoutingKey, routingKey, StringComparison.Ordinal) && e.ProjectId == projectId);

            if (match is not null)
                return match;

            var remaining = timeoutAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            await _eventArrived.WaitAsync(
                remaining < TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        throw new TimeoutException($"Event '{routingKey}' for project '{projectId}' was not captured.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();

        _eventArrived.Dispose();
    }

    private Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var json = Encoding.UTF8.GetString(args.Body.Span);
        _events.Enqueue(Parse(args.RoutingKey, json));
        _eventArrived.Release();
        return Task.CompletedTask;
    }

    private CapturedScenarioEvent Parse(string routingKey, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new CapturedScenarioEvent(
            routingKey,
            DateTimeOffset.UtcNow,
            TryGetGuid(root, "ProjectId"),
            TryGetInt32(root, "CurrentVersion"),
            TryGetInt32(root, "DeletedVersion"),
            json);

        static Guid? TryGetGuid(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
                return null;

            return Guid.TryParse(value.GetString(), out var guid) ? guid : null;
        }

        static int? TryGetInt32(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return null;

            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;
        }
    }
}
