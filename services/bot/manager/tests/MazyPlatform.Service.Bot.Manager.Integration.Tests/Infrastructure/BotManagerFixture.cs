namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

using Grpc.Core;
using Grpc.Net.Client;

using MazyPlatform.Contracts.Bot.Grpc.Manager;

using RabbitMQ.Client;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

public sealed class BotManagerFixture : IAsyncDisposable
{
    public const string ExchangeName = "mazy.exchange";
    public const string InternalAccessToken = "bot-manager-tests-internal-token";
    public const string PostgreSqlDatabase = "mazy_bot_manager_tests";
    public const string PostgreSqlPassword = "mazy-password";
    public const string PostgreSqlUser = "mazy";
    public const string RabbitMqPassword = "mazy-password";
    public const string RabbitMqUser = "mazy";
    public const string ScenarioRepositoryAccessToken = "bot-manager-tests-scenario-repository-token";

    private const string BotTokenEncryptionKey = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";

    private static readonly Lazy<BotManagerFixture> Lazy = new(() => new BotManagerFixture());

    private readonly int _internalPort = GetFreeTcpPort();
    private readonly int _publicPort = GetFreeTcpPort();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private IContainer? _botManagerContainer;
    private IFutureDockerImage? _botManagerImage;
    private GrpcChannel? _internalChannel;
    private IFutureDockerImage? _migrationImage;
    private INetwork? _network;
    private PostgreSqlContainer? _postgres;
    private GrpcChannel? _publicChannel;
    private RabbitMqContainer? _rabbitMq;
    private FakeScenarioRepositoryServer? _scenarioRepository;
    private HttpClient? _httpClient;
    private bool _started;

    private BotManagerFixture()
    {
    }

    public static BotManagerFixture Shared => Lazy.Value;

    public BotService.BotServiceClient Bots { get; private set; } = null!;

    public RabbitMqEventCapture Events { get; private set; } = null!;

    public BotManagerEventPublisher EventPublisher { get; private set; } = null!;

    public BotManagerTestFactory Factory { get; private set; } = null!;

    public BotInternalService.BotInternalServiceClient Internal { get; private set; } = null!;

    public Uri InternalAddress { get; private set; } = null!;

    public Uri PublicAddress { get; private set; } = null!;

    public FakeScenarioRepositoryServer ScenarioRepository => _scenarioRepository
        ?? throw new InvalidOperationException("Fixture is not started.");

    private string PostgreSqlContainerConnectionString =>
        $"Host=postgres;Port=5432;Database={PostgreSqlDatabase};Username={PostgreSqlUser};Password={PostgreSqlPassword}";

    public async Task StartAsync()
    {
        if (_started)
            return;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _scenarioRepository = new FakeScenarioRepositoryServer();
        await _scenarioRepository.StartAsync();

        _network = new NetworkBuilder()
            .WithName($"mazy-bot-manager-it-{_sessionId}")
            .Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithName($"mazy-bot-manager-it-postgres-{_sessionId}")
            .WithDatabase(PostgreSqlDatabase)
            .WithUsername(PostgreSqlUser)
            .WithPassword(PostgreSqlPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .Build();

        _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
            .WithName($"mazy-bot-manager-it-rabbitmq-{_sessionId}")
            .WithUsername(RabbitMqUser)
            .WithPassword(RabbitMqPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("rabbitmq")
            .Build();

        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        await DeclareExchangesAsync();

        Events = new RabbitMqEventCapture(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUser,
            RabbitMqPassword,
            ExchangeName);
        await Events.StartAsync();

        EventPublisher = new BotManagerEventPublisher(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUser,
            RabbitMqPassword,
            ExchangeName);
        await EventPublisher.StartAsync();

        await BuildImagesAsync();
        await RunMigrationsAsync();
        await StartBotManagerContainerAsync();
        CreateClients();

        Factory = new BotManagerTestFactory(this);
        _started = true;
    }

    public async Task<HttpResponseMessage> GetHealthAsync(string path)
    {
        if (_httpClient is null)
            throw new InvalidOperationException("Fixture is not started.");

        using var request = new HttpRequestMessage(HttpMethod.Get, path)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };
        request.Headers.Host = $"localhost:{_publicPort}";

        return await _httpClient.SendAsync(request);
    }

    public async Task<string> GetBotManagerLogsAsync()
    {
        if (_botManagerContainer is null)
            return string.Empty;

        var (stdout, stderr) = await _botManagerContainer.GetLogsAsync();
        return stdout + stderr;
    }

    public async Task<Exception> StartInvalidBotManagerContainerAsync()
    {
        if (_botManagerImage is null || _network is null)
            throw new InvalidOperationException("Fixture is not started.");

        var invalidPublicPort = GetFreeTcpPort();
        var invalidInternalPort = GetFreeTcpPort();
        var invalidContainer = CreateBotManagerContainerBuilder(_botManagerImage, "bot-manager-invalid", invalidPublicPort, invalidInternalPort)
            .WithEnvironment("InternalApi__AccessToken", string.Empty)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(invalidPublicPort, o => o.WithTimeout(TimeSpan.FromSeconds(20))))
            .Build();

        try
        {
            await invalidContainer.StartAsync();
            using var client = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{invalidPublicPort}"),
                Timeout = TimeSpan.FromSeconds(5),
            };
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            };
            request.Headers.Host = $"localhost:{invalidPublicPort}";
            using var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                throw new InvalidOperationException("Invalid bot-manager container became ready.");

            return new RpcException(new Status(StatusCode.Unavailable, "Invalid bot-manager container did not become ready."));
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
        _internalChannel?.Dispose();
        _publicChannel?.Dispose();
        _httpClient?.Dispose();

        if (EventPublisher is not null)
            await EventPublisher.DisposeAsync();
        if (Events is not null)
            await Events.DisposeAsync();
        if (_botManagerContainer is not null)
            await _botManagerContainer.DisposeAsync();
        if (_rabbitMq is not null)
            await _rabbitMq.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
        if (_scenarioRepository is not null)
            await _scenarioRepository.DisposeAsync();
    }

    private static string FindBotManagerRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "services", "bot", "manager");
            if (File.Exists(Path.Combine(candidate, "Dockerfile")))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find services/bot/manager from test output directory.");
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task BuildImagesAsync()
    {
        var botManagerRoot = FindBotManagerRoot();
        var botManagerImageName = $"mazy-bot-manager-it-final:{_sessionId}";
        var migrationImageName = $"mazy-bot-manager-it-migration:{_sessionId}";

        _botManagerImage = new ImageFromDockerfileBuilder()
            .WithName(botManagerImageName)
            .WithDockerfileDirectory(botManagerRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "final")
            .Build();

        _migrationImage = new ImageFromDockerfileBuilder()
            .WithName(migrationImageName)
            .WithDockerfileDirectory(botManagerRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "migration")
            .Build();

        await _botManagerImage.CreateAsync();
        await _migrationImage.CreateAsync();
    }

    private void CreateClients()
    {
        PublicAddress = new Uri($"http://localhost:{_publicPort}");
        InternalAddress = new Uri($"http://localhost:{_internalPort}");

        _publicChannel = GrpcChannel.ForAddress(PublicAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true },
        });
        _internalChannel = GrpcChannel.ForAddress(InternalAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true },
        });

        Bots = new BotService.BotServiceClient(_publicChannel);
        Internal = new BotInternalService.BotInternalServiceClient(_internalChannel);
    }

    private ContainerBuilder CreateBotManagerContainerBuilder(
        IFutureDockerImage image,
        string suffix,
        int publicPort,
        int internalPort)
    {
        return new ContainerBuilder(image)
            .WithName($"mazy-bot-manager-it-{suffix}-{_sessionId}")
            .WithNetwork(_network!)
            .WithPortBinding(publicPort, publicPort)
            .WithPortBinding(internalPort, internalPort)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MetricsPort", "0")
            .WithEnvironment("Kestrel__Endpoints__Public__Url", $"http://+:{publicPort}")
            .WithEnvironment("Kestrel__Endpoints__Public__Protocols", "Http2")
            .WithEnvironment("Kestrel__Endpoints__Internal__Url", $"http://+:{internalPort}")
            .WithEnvironment("Kestrel__Endpoints__Internal__Protocols", "Http2")
            .WithEnvironment("ConnectionStrings__DefaultConnection", PostgreSqlContainerConnectionString)
            .WithEnvironment("RabbitMq__Host", "rabbitmq")
            .WithEnvironment("RabbitMq__Port", "5672")
            .WithEnvironment("RabbitMq__Username", RabbitMqUser)
            .WithEnvironment("RabbitMq__Password", RabbitMqPassword)
            .WithEnvironment("RabbitMq__VirtualHost", "/")
            .WithEnvironment("RabbitMq__ExchangeName", ExchangeName)
            .WithEnvironment("RabbitMq__ExchangeType", ExchangeType.Topic)
            .WithEnvironment("RabbitMq__DeadLetterExchangeName", $"{ExchangeName}.dead-letter")
            .WithEnvironment("RabbitMq__Prefetch", "10")
            .WithEnvironment("InternalApi__AccessToken", InternalAccessToken)
            .WithEnvironment("BotTokenEncryption__Key", BotTokenEncryptionKey)
            .WithEnvironment("ScenarioRepository__Address", ScenarioRepository.ContainerAddress)
            .WithEnvironment("ScenarioRepository__AccessToken", ScenarioRepositoryAccessToken);
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

    private async Task RunMigrationsAsync()
    {
        if (_migrationImage is null)
            throw new InvalidOperationException("Migration image is not built.");

        var migrationContainer = new ContainerBuilder(_migrationImage)
            .WithName($"mazy-bot-manager-it-migration-{_sessionId}")
            .WithNetwork(_network!)
            .WithEnvironment("ConnectionStrings__DefaultConnection", PostgreSqlContainerConnectionString)
            .WithEnvironment("RabbitMq__Host", "rabbitmq")
            .WithEnvironment("RabbitMq__Port", "5672")
            .WithEnvironment("RabbitMq__Username", RabbitMqUser)
            .WithEnvironment("RabbitMq__Password", RabbitMqPassword)
            .WithEnvironment("RabbitMq__VirtualHost", "/")
            .WithEnvironment("RabbitMq__ExchangeName", ExchangeName)
            .WithEnvironment("RabbitMq__ExchangeType", ExchangeType.Topic)
            .WithEnvironment("RabbitMq__DeadLetterExchangeName", $"{ExchangeName}.dead-letter")
            .WithEnvironment("RabbitMq__Prefetch", "10")
            .WithEnvironment("InternalApi__AccessToken", InternalAccessToken)
            .WithEnvironment("BotTokenEncryption__Key", BotTokenEncryptionKey)
            .WithEnvironment("ScenarioRepository__Address", ScenarioRepository.ContainerAddress)
            .WithEnvironment("ScenarioRepository__AccessToken", ScenarioRepositoryAccessToken)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Done.", o => o
                    .WithMode(WaitStrategyMode.OneShot)
                    .WithTimeout(TimeSpan.FromMinutes(3))))
            .Build();

        await using (migrationContainer)
        {
            await migrationContainer.StartAsync();
        }
    }

    private async Task StartBotManagerContainerAsync()
    {
        if (_botManagerImage is null)
            throw new InvalidOperationException("Bot manager image is not built.");

        _botManagerContainer = CreateBotManagerContainerBuilder(_botManagerImage, "bot-manager", _publicPort, _internalPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(_publicPort, o => o.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        await _botManagerContainer.StartAsync();
        PublicAddress = new Uri($"http://localhost:{_publicPort}");
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
        })
        {
            BaseAddress = PublicAddress,
            Timeout = TimeSpan.FromSeconds(15),
        };
        await WaitForReadyAsync();
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

        var logs = string.Empty;
        if (_botManagerContainer is not null)
        {
            var (stdout, stderr) = await _botManagerContainer.GetLogsAsync();
            logs = stdout + stderr;
        }

        throw new TimeoutException(
            $"Bot manager did not become ready. Last exception: {lastException?.Message}. Logs: {logs}");
    }
}
