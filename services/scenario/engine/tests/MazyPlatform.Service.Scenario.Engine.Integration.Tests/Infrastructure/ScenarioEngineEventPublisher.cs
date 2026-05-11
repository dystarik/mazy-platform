namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using System.Reflection;
using System.Text;
using System.Text.Json;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Integration.Events;
using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Contracts.Core;

using RabbitMQ.Client;

public sealed class ScenarioEngineEventPublisher : IAsyncDisposable
{
    private readonly string _exchangeName;
    private readonly string _host;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;
    private IChannel? _channel;
    private IConnection? _connection;

    public ScenarioEngineEventPublisher(string host, int port, string username, string password, string exchangeName)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _exchangeName = exchangeName;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    public async Task PublishActivatedAsync(
        Guid botInstanceId,
        PlatformType platformType = PlatformType.Telegram,
        Guid? projectId = null,
        int scenarioVersion = 1)
    {
        await PublishAsync(new BotInstanceActivatedIntegrationEvent(
            DateTimeOffset.UtcNow,
            botInstanceId,
            projectId ?? Guid.NewGuid(),
            platformType,
            platformType == PlatformType.Vk ? "12345" : null,
            scenarioVersion));
    }

    public async Task PublishDeactivatedAsync(Guid botInstanceId)
    {
        await PublishAsync(new BotInstanceDeactivatedIntegrationEvent(DateTimeOffset.UtcNow, botInstanceId));
    }

    public async Task PublishDeletedAsync(Guid botInstanceId)
    {
        await PublishAsync(new BotInstanceDeletedIntegrationEvent(DateTimeOffset.UtcNow, botInstanceId, Guid.NewGuid()));
    }

    public async Task PublishIncomingAsync(
        Guid botInstanceId,
        PlatformType platformType = PlatformType.Telegram,
        Guid? projectId = null,
        int scenarioVersion = 1,
        string? rawPayload = null)
    {
        var payload = rawPayload ?? (platformType == PlatformType.Vk
            ? ScenarioEngineTestData.VkMessagePayload()
            : ScenarioEngineTestData.TelegramMessagePayload());
        await PublishAsync(new BotIncomingEventIntegrationEvent(
            DateTimeOffset.UtcNow,
            platformType,
            botInstanceId,
            projectId ?? Guid.NewGuid(),
            scenarioVersion,
            payload));
    }

    public async Task PublishRawToExchangeAsync(string routingKey, string json)
    {
        EnsureStarted();
        await _channel!.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: false,
            body: Encoding.UTF8.GetBytes(json));
    }

    public async Task PublishRawToQueueAsync(string queueName, string json)
    {
        EnsureStarted();
        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            body: Encoding.UTF8.GetBytes(json));
    }

    public async Task PublishTokenChangedAsync(Guid botInstanceId)
    {
        await PublishAsync(new BotInstanceTokenChangedIntegrationEvent(DateTimeOffset.UtcNow, botInstanceId));
    }

    public async Task PublishUnboundAsync(Guid botInstanceId)
    {
        await PublishAsync(new BotInstanceUnboundFromProjectIntegrationEvent(DateTimeOffset.UtcNow, botInstanceId, Guid.NewGuid()));
    }

    public async Task PublishVersionChangedAsync(Guid botInstanceId, int newScenarioVersion = 2)
    {
        await PublishAsync(new BotInstanceScenarioVersionChangedIntegrationEvent(DateTimeOffset.UtcNow, botInstanceId, newScenarioVersion));
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

    private void EnsureStarted()
    {
        if (_channel is null)
            throw new InvalidOperationException("Publisher is not started.");
    }

    private async Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : IIntegrationEvent
    {
        var attribute = typeof(TEvent).GetCustomAttribute<IntegrationEventTypeAttribute>();
        var routingKey = attribute?.RoutingKey
            ?? throw new InvalidOperationException($"Event {typeof(TEvent).Name} has no IntegrationEventTypeAttribute.");
        var json = JsonSerializer.Serialize(@event);
        await PublishRawToExchangeAsync(routingKey, json);
    }
}
