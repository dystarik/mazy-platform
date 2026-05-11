namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using System.Reflection;
using System.Text;
using System.Text.Json;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Contracts.User.Authentication.Events;

using RabbitMQ.Client;

public sealed class BotManagerEventPublisher : IAsyncDisposable
{
    private readonly string _exchangeName;
    private readonly string _host;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;
    private IChannel? _channel;
    private IConnection? _connection;

    public BotManagerEventPublisher(string host, int port, string username, string password, string exchangeName)
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

        var body = Encoding.UTF8.GetBytes("""{"UserAccountId":"not-a-guid","Email":42}""");
        await _channel.BasicPublishAsync(
            _exchangeName,
            RoutingKeyOf<UserAccountEmailConfirmedIntegrationEvent>(),
            body);
    }

    public async Task PublishProjectDeletedAsync(Guid projectId)
    {
        await PublishAsync(new ProjectDeletedIntegrationEvent(DateTimeOffset.UtcNow, projectId));
    }

    public async Task PublishReleaseChangedAsync(Guid projectId, int currentVersion)
    {
        await PublishAsync(new ScenarioReleaseChangedIntegrationEvent(DateTimeOffset.UtcNow, projectId, currentVersion));
    }

    public async Task PublishReleaseRemovedAsync(Guid projectId)
    {
        await PublishAsync(new ScenarioReleaseRemovedIntegrationEvent(DateTimeOffset.UtcNow, projectId));
    }

    public async Task PublishUnknownAsync()
    {
        if (_channel is null)
            throw new InvalidOperationException("Publisher is not started.");

        var body = Encoding.UTF8.GetBytes("""{"Ignored":true}""");
        await _channel.BasicPublishAsync(_exchangeName, "bot.manager.integration-tests.unknown", body);
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

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
        await _channel.BasicPublishAsync(_exchangeName, RoutingKeyOf<TEvent>(), body);
    }
}
