namespace MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

using RabbitMQ.Client;

using Testcontainers.RabbitMq;

public sealed class BotIntegrationFixture : IAsyncDisposable
{
    public const string BotManagerAccessToken = "bot-integration-tests-bot-manager-token";
    public const string ExchangeName = "mazy.exchange";
    public const string RabbitMqPassword = "mazy-password";
    public const string RabbitMqUser = "mazy";

    private static readonly Lazy<BotIntegrationFixture> Lazy = new(() => new BotIntegrationFixture());

    private readonly int _metricsPort = TestPorts.GetFreeTcpPort();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private IContainer? _botIntegrationContainer;
    private IFutureDockerImage? _botIntegrationImage;
    private FakeBotManagerServer? _botManager;
    private HttpClient? _httpClient;
    private INetwork? _network;
    private RabbitMqContainer? _rabbitMq;
    private bool _started;

    public static BotIntegrationFixture Shared => Lazy.Value;

    public BotIntegrationEventPublisher EventPublisher { get; private set; } = null!;

    public FakeBotManagerServer BotManager => _botManager
        ?? throw new InvalidOperationException("Fixture is not started.");

    public RabbitMqQueueInspector Queues { get; private set; } = null!;

    public static async Task<BotIntegrationFixture> StartIsolatedAsync(Action<FakeBotManagerServer>? configureBotManager = null)
    {
        var fixture = new BotIntegrationFixture();
        await fixture.StartAsync(configureBotManager);
        return fixture;
    }

    public async Task StartAsync()
    {
        if (_started)
            return;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _botManager = new FakeBotManagerServer();
        await _botManager.StartAsync();

        _network = new NetworkBuilder()
            .WithName($"mazy-bot-integration-it-{_sessionId}")
            .Build();
        await _network.CreateAsync();

        _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
            .WithName($"mazy-bot-integration-it-rabbitmq-{_sessionId}")
            .WithUsername(RabbitMqUser)
            .WithPassword(RabbitMqPassword)
            .WithPortBinding(15672, true)
            .WithNetwork(_network)
            .WithNetworkAliases("rabbitmq")
            .Build();

        await _rabbitMq.StartAsync();
        await DeclareExchangesAsync();

        EventPublisher = new BotIntegrationEventPublisher(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUser,
            RabbitMqPassword,
            ExchangeName);
        await EventPublisher.StartAsync();

        Queues = new RabbitMqQueueInspector(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(15672),
            RabbitMqUser,
            RabbitMqPassword);

        await BuildImageAsync();
        await StartBotIntegrationContainerAsync();
        _started = true;
    }

    public async Task<HttpResponseMessage> GetHealthAsync(string path)
    {
        if (_httpClient is null)
            throw new InvalidOperationException("Fixture is not started.");

        return await _httpClient.GetAsync(path);
    }

    public async Task<string> GetBotIntegrationLogsAsync()
    {
        if (_botIntegrationContainer is null)
            return string.Empty;

        var (stdout, stderr) = await _botIntegrationContainer.GetLogsAsync();
        return stdout + stderr;
    }

    public async Task<Exception> StartInvalidContainerAsync(string key, string value)
    {
        if (_botIntegrationImage is null || _network is null)
            throw new InvalidOperationException("Fixture is not started.");

        var invalidPort = TestPorts.GetFreeTcpPort();
        var invalidContainer = CreateContainerBuilder("invalid", invalidPort)
            .WithEnvironment(key, value)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(invalidPort, o => o.WithTimeout(TimeSpan.FromSeconds(20))))
            .Build();

        try
        {
            await invalidContainer.StartAsync();
            using var client = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{invalidPort}"),
                Timeout = TimeSpan.FromSeconds(5),
            };

            using var response = await client.GetAsync("/health/ready");
            if (response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Invalid bot-integration container became ready with '{key}'.");

            return new InvalidOperationException("Invalid bot-integration container did not become ready.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            return ex;
        }
        finally
        {
            await invalidContainer.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();

        if (EventPublisher is not null)
            await EventPublisher.DisposeAsync();
        Queues?.Dispose();
        if (_botIntegrationContainer is not null)
            await _botIntegrationContainer.DisposeAsync();
        if (_rabbitMq is not null)
            await _rabbitMq.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
        if (_botManager is not null)
            await _botManager.DisposeAsync();
    }

    private async Task StartAsync(Action<FakeBotManagerServer>? configureBotManager)
    {
        if (_started)
            return;

        _botManager = new FakeBotManagerServer();
        configureBotManager?.Invoke(_botManager);
        await StartAsync();
    }

    private async Task BuildImageAsync()
    {
        var botIntegrationRoot = TestPaths.FindBotIntegrationRoot();
        var botIntegrationImageName = $"mazy-bot-integration-it-final:{_sessionId}";

        _botIntegrationImage = new ImageFromDockerfileBuilder()
            .WithName(botIntegrationImageName)
            .WithDockerfileDirectory(botIntegrationRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "final")
            .Build();

        await _botIntegrationImage.CreateAsync();
    }

    private ContainerBuilder CreateContainerBuilder(string suffix, int metricsPort)
    {
        return new ContainerBuilder(_botIntegrationImage!)
            .WithName($"mazy-bot-integration-it-{suffix}-{_sessionId}")
            .WithNetwork(_network!)
            .WithPortBinding(metricsPort, metricsPort)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MetricsPort", metricsPort.ToString(CultureInfo.InvariantCulture))
            .WithEnvironment("RabbitMq__Host", "rabbitmq")
            .WithEnvironment("RabbitMq__Port", "5672")
            .WithEnvironment("RabbitMq__Username", RabbitMqUser)
            .WithEnvironment("RabbitMq__Password", RabbitMqPassword)
            .WithEnvironment("RabbitMq__VirtualHost", "/")
            .WithEnvironment("RabbitMq__ExchangeName", ExchangeName)
            .WithEnvironment("RabbitMq__ExchangeType", ExchangeType.Topic)
            .WithEnvironment("RabbitMq__DeadLetterExchangeName", $"{ExchangeName}.dead-letter")
            .WithEnvironment("RabbitMq__Prefetch", "1")
            .WithEnvironment("BotManager__Address", BotManager.ContainerAddress)
            .WithEnvironment("BotManager__AccessToken", BotManagerAccessToken)
            .WithEnvironment("Vk__ApiVersion", "5.199")
            .WithEnvironment("Vk__WaitSeconds", "1")
            .WithEnvironment("Telegram__TimeoutSeconds", "1")
            .WithEnvironment("Telegram__RetryDelaySeconds", "1");
    }

    private async Task DeclareExchangesAsync()
    {
        if (_rabbitMq is null)
            throw new InvalidOperationException("RabbitMQ container is not started.");

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMq.Hostname,
            Port = _rabbitMq.GetMappedPublicPort(5672),
            UserName = RabbitMqUser,
            Password = RabbitMqPassword,
            VirtualHost = "/",
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true);
        await channel.ExchangeDeclareAsync($"{ExchangeName}.dead-letter", ExchangeType.Direct, durable: true);
    }

    private async Task StartBotIntegrationContainerAsync()
    {
        if (_botIntegrationImage is null)
            throw new InvalidOperationException("Bot integration image is not built.");

        _botIntegrationContainer = CreateContainerBuilder("bot-integration", _metricsPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(_metricsPort, o => o.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        await _botIntegrationContainer.StartAsync();
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
        })
        {
            BaseAddress = new Uri($"http://localhost:{_metricsPort}"),
            Timeout = TimeSpan.FromSeconds(15),
        };
        await WaitForReadyAsync();
        foreach (var queue in BotIntegrationQueues.Main)
        {
            await Queues.WaitForQueueExistsAsync(queue);
            await Queues.WaitForQueueExistsAsync(BotIntegrationQueues.DeadLetter(queue));
        }
    }

    private async Task WaitForReadyAsync()
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddMinutes(2);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                using var response = await GetHealthAsync("/health/ready");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        var logs = await GetBotIntegrationLogsAsync();
        throw new TimeoutException(
            $"Bot integration did not become ready. Last exception: {lastException?.Message}. Logs: {logs}");
    }

    private static class TestPaths
    {
        public static string FindBotIntegrationRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "services", "bot", "integration");
                if (File.Exists(Path.Combine(candidate, "Dockerfile")))
                    return candidate;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not find services/bot/integration from test output directory.");
        }
    }

    private static class TestPorts
    {
        public static int GetFreeTcpPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}
