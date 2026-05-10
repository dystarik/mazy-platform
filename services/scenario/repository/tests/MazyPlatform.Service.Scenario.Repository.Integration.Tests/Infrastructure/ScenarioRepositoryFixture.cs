namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

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

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

using RabbitMQ.Client;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

public sealed class ScenarioRepositoryFixture : IAsyncDisposable
{
    public const string BotManagerAccessToken = "scenario-repository-tests-bot-manager-token";
    public const string ExchangeName = "mazy.exchange";
    public const string InternalAccessToken = "scenario-repository-tests-internal-token";
    public const string MongoDatabase = "mazy_scenario_repository_tests";
    public const string PostgreSqlDatabase = "mazy_scenario_repository_tests";
    public const string PostgreSqlPassword = "mazy-password";
    public const string PostgreSqlUser = "mazy";
    public const string RabbitMqPassword = "mazy-password";
    public const string RabbitMqUser = "mazy";

    private static readonly Lazy<ScenarioRepositoryFixture> Lazy = new(() => new ScenarioRepositoryFixture());

    private readonly int _internalPort = GetFreeTcpPort();
    private readonly int _publicPort = GetFreeTcpPort();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private FakeBotManagerServer? _botManager;
    private GrpcChannel? _internalChannel;
    private IFutureDockerImage? _migrationImage;
    private IContainer? _mongo;
    private INetwork? _network;
    private PostgreSqlContainer? _postgres;
    private GrpcChannel? _publicChannel;
    private RabbitMqContainer? _rabbitMq;
    private IContainer? _repositoryContainer;
    private IFutureDockerImage? _repositoryImage;
    private HttpClient? _httpClient;
    private bool _started;

    private ScenarioRepositoryFixture()
    {
    }

    public static ScenarioRepositoryFixture Shared => Lazy.Value;

    public EntitySchemaService.EntitySchemaServiceClient EntitySchemas { get; private set; } = null!;

    public RabbitMqEventCapture Events { get; private set; } = null!;

    public ScenarioRepositoryEventPublisher EventPublisher { get; private set; } = null!;

    public FakeBotManagerServer BotManager => _botManager
        ?? throw new InvalidOperationException("Fixture is not started.");

    public ScenarioRepositoryInternalService.ScenarioRepositoryInternalServiceClient Internal { get; private set; } = null!;

    public ProjectService.ProjectServiceClient Projects { get; private set; } = null!;

    public ScenarioGraphService.ScenarioGraphServiceClient ScenarioGraphs { get; private set; } = null!;

    public ScenarioRepositoryTestFactory Factory { get; private set; } = null!;

    public UserDataService.UserDataServiceClient UserData { get; private set; } = null!;

    public Uri InternalAddress { get; private set; } = null!;

    public string MongoHostConnectionString => $"mongodb://localhost:{_mongo?.GetMappedPublicPort(27017)}";

    public Uri PublicAddress { get; private set; } = null!;

    private string MongoContainerConnectionString => "mongodb://mongo:27017";

    private string PostgreSqlContainerConnectionString =>
        $"Host=postgres;Port=5432;Database={PostgreSqlDatabase};Username={PostgreSqlUser};Password={PostgreSqlPassword}";

    public async Task StartAsync()
    {
        if (_started)
            return;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _botManager = new FakeBotManagerServer();
        await _botManager.StartAsync();

        _network = new NetworkBuilder()
            .WithName($"mazy-scenario-repository-it-{_sessionId}")
            .Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithName($"mazy-scenario-repository-it-postgres-{_sessionId}")
            .WithDatabase(PostgreSqlDatabase)
            .WithUsername(PostgreSqlUser)
            .WithPassword(PostgreSqlPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .Build();

        _mongo = new ContainerBuilder("mongo:7")
            .WithName($"mazy-scenario-repository-it-mongo-{_sessionId}")
            .WithNetwork(_network)
            .WithNetworkAliases("mongo")
            .WithPortBinding(27017, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017, o => o.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
            .WithName($"mazy-scenario-repository-it-rabbitmq-{_sessionId}")
            .WithUsername(RabbitMqUser)
            .WithPassword(RabbitMqPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("rabbitmq")
            .Build();

        await _postgres.StartAsync();
        await _mongo.StartAsync();
        await _rabbitMq.StartAsync();
        await DeclareExchangesAsync();

        Events = new RabbitMqEventCapture(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUser,
            RabbitMqPassword,
            ExchangeName);
        await Events.StartAsync();

        EventPublisher = new ScenarioRepositoryEventPublisher(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUser,
            RabbitMqPassword,
            ExchangeName);
        await EventPublisher.StartAsync();

        await BuildImagesAsync();
        await RunMigrationsAsync();
        await StartRepositoryContainerAsync();
        CreateClients();

        Factory = new ScenarioRepositoryTestFactory(this);
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

    public async Task<string> GetRepositoryLogsAsync()
    {
        if (_repositoryContainer is null)
            return string.Empty;

        var (stdout, stderr) = await _repositoryContainer.GetLogsAsync();
        return stdout + stderr;
    }

    public async Task<Exception> StartInvalidRepositoryContainerAsync()
    {
        if (_repositoryImage is null || _network is null)
            throw new InvalidOperationException("Fixture is not started.");

        var invalidPublicPort = GetFreeTcpPort();
        var invalidInternalPort = GetFreeTcpPort();
        var invalidContainer = CreateRepositoryContainerBuilder(_repositoryImage, "repository-invalid", invalidPublicPort, invalidInternalPort)
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
                throw new InvalidOperationException("Invalid scenario-repository container became ready.");

            return new RpcException(new Status(StatusCode.Unavailable, "Invalid scenario-repository container did not become ready."));
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
        if (_repositoryContainer is not null)
            await _repositoryContainer.DisposeAsync();
        if (_rabbitMq is not null)
            await _rabbitMq.DisposeAsync();
        if (_mongo is not null)
            await _mongo.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
        if (_botManager is not null)
            await _botManager.DisposeAsync();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "services", "scenario", "repository");
            if (File.Exists(Path.Combine(candidate, "Dockerfile")))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find services/scenario/repository from test output directory.");
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task BuildImagesAsync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var repositoryImageName = $"mazy-scenario-repository-it-final:{_sessionId}";
        var migrationImageName = $"mazy-scenario-repository-it-migration:{_sessionId}";

        _repositoryImage = new ImageFromDockerfileBuilder()
            .WithName(repositoryImageName)
            .WithDockerfileDirectory(repositoryRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "final")
            .Build();

        _migrationImage = new ImageFromDockerfileBuilder()
            .WithName(migrationImageName)
            .WithDockerfileDirectory(repositoryRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "migration")
            .Build();

        await _repositoryImage.CreateAsync();
        await _migrationImage.CreateAsync();
    }

    private void CreateClients()
    {
        PublicAddress = new Uri($"http://localhost:{_publicPort}");
        InternalAddress = new Uri($"http://localhost:{_internalPort}");

        var publicHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
        };
        var internalHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
        };

        _publicChannel = GrpcChannel.ForAddress(PublicAddress, new GrpcChannelOptions
        {
            HttpHandler = publicHandler,
        });
        _internalChannel = GrpcChannel.ForAddress(InternalAddress, new GrpcChannelOptions
        {
            HttpHandler = internalHandler,
        });

        Projects = new ProjectService.ProjectServiceClient(_publicChannel);
        EntitySchemas = new EntitySchemaService.EntitySchemaServiceClient(_publicChannel);
        ScenarioGraphs = new ScenarioGraphService.ScenarioGraphServiceClient(_publicChannel);
        UserData = new UserDataService.UserDataServiceClient(_publicChannel);
        Internal = new ScenarioRepositoryInternalService.ScenarioRepositoryInternalServiceClient(_internalChannel);
    }

    private ContainerBuilder CreateRepositoryContainerBuilder(
        IFutureDockerImage image,
        string suffix,
        int publicPort,
        int internalPort)
    {
        return new ContainerBuilder(image)
            .WithName($"mazy-scenario-repository-it-{suffix}-{_sessionId}")
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
            .WithEnvironment("MongoDb__ConnectionString", MongoContainerConnectionString)
            .WithEnvironment("MongoDb__DatabaseName", MongoDatabase)
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
            .WithEnvironment("BotManager__Address", BotManager.ContainerAddress)
            .WithEnvironment("BotManager__AccessToken", BotManagerAccessToken);
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
            .WithName($"mazy-scenario-repository-it-migration-{_sessionId}")
            .WithNetwork(_network!)
            .WithEnvironment("ConnectionStrings__DefaultConnection", PostgreSqlContainerConnectionString)
            .WithEnvironment("MongoDb__ConnectionString", MongoContainerConnectionString)
            .WithEnvironment("MongoDb__DatabaseName", MongoDatabase)
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
            .WithEnvironment("BotManager__Address", BotManager.ContainerAddress)
            .WithEnvironment("BotManager__AccessToken", BotManagerAccessToken)
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

    private async Task StartRepositoryContainerAsync()
    {
        if (_repositoryImage is null)
            throw new InvalidOperationException("Repository image is not built.");

        _repositoryContainer = CreateRepositoryContainerBuilder(_repositoryImage, "repository", _publicPort, _internalPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(_publicPort, o => o.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        await _repositoryContainer.StartAsync();
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
        if (_repositoryContainer is not null)
        {
            var (stdout, stderr) = await _repositoryContainer.GetLogsAsync();
            logs = stdout + stderr;
        }

        throw new TimeoutException(
            $"Scenario repository did not become ready. Last exception: {lastException?.Message}. Logs: {logs}");
    }
}
