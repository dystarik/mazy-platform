namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public sealed class RabbitMqEventCapture : IAsyncDisposable
{
    public const string Registered = "user.authentication.registered";
    public const string EmailConfirmed = "user.authentication.email-confirmed";
    public const string MfaEmailCodeGenerated = "user.authentication.mfa-email-code-generated";
    public const string PasswordResetRequested = "user.authentication.password-reset-requested";

    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _exchangeName;
    private readonly ConcurrentQueue<CapturedAuthEvent> _events = new();
    private readonly SemaphoreSlim _eventArrived = new(0);
    private IConnection? _connection;
    private IChannel? _channel;
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
        await _channel.QueueBindAsync(_queueName, _exchangeName, Registered);
        await _channel.QueueBindAsync(_queueName, _exchangeName, EmailConfirmed);
        await _channel.QueueBindAsync(_queueName, _exchangeName, MfaEmailCodeGenerated);
        await _channel.QueueBindAsync(_queueName, _exchangeName, PasswordResetRequested);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;
        await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer);
    }

    public async Task<CapturedAuthEvent> WaitForAsync(string routingKey, string email, int afterCount, CancellationToken cancellationToken = default)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var current = _events.ToArray();
            var match = current
                .Skip(afterCount)
                .FirstOrDefault(e =>
                    string.Equals(e.RoutingKey, routingKey, StringComparison.Ordinal) &&
                    string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
                return match;

            var remaining = timeoutAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            await _eventArrived.WaitAsync(
                remaining < TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        throw new TimeoutException($"Event '{routingKey}' for email '{email}' was not captured.");
    }

    public async Task<bool> HasAnyEventAfterAsync(int afterCount, TimeSpan wait, CancellationToken cancellationToken = default)
    {
        var timeoutAt = DateTimeOffset.UtcNow.Add(wait);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (_events.Count > afterCount)
                return true;

            var remaining = timeoutAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            await _eventArrived.WaitAsync(
                remaining < TimeSpan.FromMilliseconds(100) ? remaining : TimeSpan.FromMilliseconds(100),
                cancellationToken);
        }

        return _events.Count > afterCount;
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
        var captured = Parse(args.RoutingKey, json);
        _events.Enqueue(captured);
        _eventArrived.Release();
        return Task.CompletedTask;
    }

    private CapturedAuthEvent Parse(string routingKey, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new CapturedAuthEvent(
            routingKey,
            DateTimeOffset.UtcNow,
            TryGetGuid(root, "UserAccountId"),
            TryGetString(root, "Email"),
            TryGetString(root, "ConfirmationCode"),
            TryGetString(root, "Code"),
            TryGetString(root, "ResetCode"),
            json);
    }

    private string? TryGetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private Guid? TryGetGuid(JsonElement root, string propertyName)
    {
        var value = TryGetString(root, propertyName);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
