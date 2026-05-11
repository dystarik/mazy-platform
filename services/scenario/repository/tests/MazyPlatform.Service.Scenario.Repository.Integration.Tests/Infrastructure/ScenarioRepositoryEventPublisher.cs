namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using System.Reflection;
using System.Text;
using System.Text.Json;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.User.Authentication.Events;

using RabbitMQ.Client;

public sealed class ScenarioRepositoryEventPublisher : IAsyncDisposable
{
    private readonly string _exchangeName;
    private readonly string _host;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;
    private IChannel? _channel;
    private IConnection? _connection;

    public ScenarioRepositoryEventPublisher(string host, int port, string username, string password, string exchangeName)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _exchangeName = exchangeName;
    }

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
    }

    public async Task PublishEmailConfirmedAsync(Guid userAccountId)
    {
        var @event = new UserAccountEmailConfirmedIntegrationEvent(
            DateTimeOffset.UtcNow,
            userAccountId,
            $"user-{userAccountId:N}@mazy.test");

        await PublishAsync(@event);
    }

    public async Task PublishMalformedEmailConfirmedAsync()
    {
        if (_channel is null)
            throw new InvalidOperationException("Publisher is not started.");

        var routingKey = RoutingKeyOf<UserAccountEmailConfirmedIntegrationEvent>();
        var body = Encoding.UTF8.GetBytes("""{"UserAccountId":"not-a-guid","Email":42}""");
        await _channel.BasicPublishAsync(_exchangeName, routingKey, body);
    }

    public async Task PublishUnknownAsync()
    {
        if (_channel is null)
            throw new InvalidOperationException("Publisher is not started.");

        var body = Encoding.UTF8.GetBytes("""{"Ignored":true}""");
        await _channel.BasicPublishAsync(_exchangeName, "scenario.repository.integration-tests.unknown", body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    private static string RoutingKeyOf<TEvent>()
    {
        var attribute = typeof(TEvent).GetCustomAttribute<IntegrationEventTypeAttribute>();
        return attribute?.RoutingKey
            ?? throw new InvalidOperationException($"Event {typeof(TEvent).Name} has no IntegrationEventTypeAttribute.");
    }

    private async Task PublishAsync<TEvent>(TEvent @event)
    {
        if (_channel is null)
            throw new InvalidOperationException("Publisher is not started.");

        var routingKey = RoutingKeyOf<TEvent>();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
        await _channel.BasicPublishAsync(_exchangeName, routingKey, body);
    }
}
