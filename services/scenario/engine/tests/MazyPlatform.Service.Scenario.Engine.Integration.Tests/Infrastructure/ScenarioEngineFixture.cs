namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

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

using MongoDB.Bson;
using MongoDB.Driver;

using RabbitMQ.Client;

using Testcontainers.RabbitMq;

public sealed class ScenarioEngineFixture : IAsyncDisposable
{
    public const string BotManagerAccessToken = "scenario-engine-tests-bot-manager-token";
    public const string ExchangeName = "mazy.exchange";
    public const string MongoDatabaseName = "mazy_scenario_engine_it";
    public const string RabbitMqPassword = "mazy-password";
    public const string RabbitMqUser = "mazy";

    private const string SessionsCollectionName = "sessions";

    private static readonly Lazy<ScenarioEngineFixture> Lazy = new(() => new ScenarioEngineFixture());

    private readonly int _metricsPort = TestPorts.GetFreeTcpPort();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private FakeBotManagerServer? _botManager;
    private HttpClient? _httpClient;
    private IContainer? _mongo;
    private MongoClient? _mongoClient;
    private INetwork? _network;
    private RabbitMqContainer? _rabbitMq;
    private IContainer? _scenarioEngineContainer;
    private IFutureDockerImage? _scenarioEngineImage;
    private FakeScenarioRepositoryServer? _scenarioRepository;
    private bool _started;

    public static ScenarioEngineFixture Shared => Lazy.Value;

    public FakeBotManagerServer BotManager => _botManager
        ?? throw new InvalidOperationException("Fixture is not started.");

    public ScenarioEngineEventPublisher EventPublisher { get; private set; } = null!;

    public RabbitMqQueueInspector Queues { get; private set; } = null!;

    public FakeScenarioRepositoryServer ScenarioRepository => _scenarioRepository
        ?? throw new InvalidOperationException("Fixture is not started.");

    public static async Task<ScenarioEngineFixture> StartIsolatedAsync(
        Action<FakeBotManagerServer>? configureBotManager = null,
        Action<FakeScenarioRepositoryServer>? configureScenarioRepository = null,
        bool delayResumeEnabled = true,
        int delayResumeBatchSize = 20)
    {
        var fixture = new ScenarioEngineFixture();
        await fixture.StartAsync(configureBotManager, configureScenarioRepository, delayResumeEnabled, delayResumeBatchSize);
        return fixture;
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();

        if (EventPublisher is not null)
            await EventPublisher.DisposeAsync();
        Queues?.Dispose();
        if (_scenarioEngineContainer is not null)
            await _scenarioEngineContainer.DisposeAsync();
        if (_mongo is not null)
            await _mongo.DisposeAsync();
        if (_rabbitMq is not null)
            await _rabbitMq.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
        if (_botManager is not null)
            await _botManager.DisposeAsync();
        if (_scenarioRepository is not null)
            await _scenarioRepository.DisposeAsync();
    }

    public async Task<BsonDocument?> FindSessionAsync(Guid botId, string platformUserId)
    {
        var collection = GetSessionsCollection();
        var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return documents
            .Where(x =>
                ReadGuid(x.GetValue("botId")) == botId
                && string.Equals(x.GetValue("platformUserId", string.Empty).AsString, platformUserId, StringComparison.Ordinal))
            .OrderByDescending(x => x.GetValue("updatedAt", BsonNull.Value).ToUniversalTime())
            .FirstOrDefault();
    }

    public async Task<HttpResponseMessage> GetHealthAsync(string path)
    {
        if (_httpClient is null)
            throw new InvalidOperationException("Fixture is not started.");

        return await _httpClient.GetAsync(path);
    }

    public async Task<string> GetScenarioEngineLogsAsync()
    {
        if (_scenarioEngineContainer is null)
            return string.Empty;

        var (stdout, stderr) = await _scenarioEngineContainer.GetLogsAsync();
        return stdout + stderr;
    }

    public async Task<bool> HasSessionIndexAsync(string indexName)
    {
        var indexes = await GetSessionsCollection().Indexes.ListAsync();
        var documents = await indexes.ToListAsync();
        return documents.Any(x =>
            x.TryGetValue("name", out var name)
            && string.Equals(name.AsString, indexName, StringComparison.Ordinal));
    }

    public async Task<int> CountLockedSessionsAsync(params string[] platformUserIds)
    {
        var sessions = await GetSessionsCollection().Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return sessions.Count(x =>
            platformUserIds.Contains(x.GetValue("platformUserId", string.Empty).AsString, StringComparer.Ordinal)
            && x.Contains("delayResumeLockUntil"));
    }

    public async Task InsertDelaySessionAsync(Guid botId, string platformUserId, DateTimeOffset resumeAt, bool lockInFuture = false)
    {
        var now = DateTime.UtcNow;
        var variables = new BsonDocument
        {
            ["__delay_resume_at"] = resumeAt.ToString("O", CultureInfo.InvariantCulture),
            ["_chatId"] = platformUserId,
        };
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["botId"] = new BsonBinaryData(botId, GuidRepresentation.Standard),
            ["platformUserId"] = platformUserId,
            ["currentNodeId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["state"] = "WaitingForEvent",
            ["variables"] = variables,
            ["createdAt"] = now,
            ["updatedAt"] = now,
        };

        if (lockInFuture)
            document["delayResumeLockUntil"] = DateTime.UtcNow.AddMinutes(5);

        await GetSessionsCollection().InsertOneAsync(document);
    }

    public async Task InsertDelaySessionAsync(
        Guid botId,
        string platformUserId,
        Guid currentNodeId,
        DateTimeOffset resumeAt,
        bool lockInFuture = false)
    {
        var now = DateTime.UtcNow;
        var variables = new BsonDocument
        {
            ["__delay_resume_at"] = resumeAt.ToString("O", CultureInfo.InvariantCulture),
            ["_chatId"] = platformUserId,
        };
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["botId"] = new BsonBinaryData(botId, GuidRepresentation.Standard),
            ["platformUserId"] = platformUserId,
            ["currentNodeId"] = new BsonBinaryData(currentNodeId, GuidRepresentation.Standard),
            ["state"] = "WaitingForEvent",
            ["variables"] = variables,
            ["createdAt"] = now,
            ["updatedAt"] = now,
        };

        if (lockInFuture)
            document["delayResumeLockUntil"] = DateTime.UtcNow.AddMinutes(5);

        await GetSessionsCollection().InsertOneAsync(document);
    }

    public async Task InsertDelaySessionWithLockAsync(
        Guid botId,
        string platformUserId,
        DateTimeOffset resumeAt,
        DateTime lockUntil)
    {
        var now = DateTime.UtcNow;
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["botId"] = new BsonBinaryData(botId, GuidRepresentation.Standard),
            ["platformUserId"] = platformUserId,
            ["currentNodeId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["state"] = "WaitingForEvent",
            ["variables"] = new BsonDocument { ["__delay_resume_at"] = resumeAt.ToString("O", CultureInfo.InvariantCulture) },
            ["createdAt"] = now,
            ["updatedAt"] = now,
            ["delayResumeLockUntil"] = lockUntil,
        };

        await GetSessionsCollection().InsertOneAsync(document);
    }

    public async Task InsertMalformedDelaySessionAsync(Guid botId, string platformUserId, string resumeAt)
    {
        var now = DateTime.UtcNow;
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["botId"] = new BsonBinaryData(botId, GuidRepresentation.Standard),
            ["platformUserId"] = platformUserId,
            ["currentNodeId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["state"] = "WaitingForEvent",
            ["variables"] = new BsonDocument { ["__delay_resume_at"] = resumeAt },
            ["createdAt"] = now,
            ["updatedAt"] = now,
        };

        await GetSessionsCollection().InsertOneAsync(document);
    }

    public async Task InsertSessionAsync(
        Guid botId,
        string platformUserId,
        string state,
        Guid? currentNodeId,
        BsonDocument? variables = null)
    {
        var now = DateTime.UtcNow;
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["botId"] = new BsonBinaryData(botId, GuidRepresentation.Standard),
            ["platformUserId"] = platformUserId,
            ["state"] = state,
            ["variables"] = variables ?? [],
            ["createdAt"] = now,
            ["updatedAt"] = now,
        };

        if (currentNodeId.HasValue)
            document["currentNodeId"] = new BsonBinaryData(currentNodeId.Value, GuidRepresentation.Standard);

        await GetSessionsCollection().InsertOneAsync(document);
    }

    public async Task InsertWaitingSessionAsync(Guid botId, string platformUserId, Guid currentNodeId, BsonDocument? variables = null)
    {
        var now = DateTime.UtcNow;
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["botId"] = new BsonBinaryData(botId, GuidRepresentation.Standard),
            ["platformUserId"] = platformUserId,
            ["currentNodeId"] = new BsonBinaryData(currentNodeId, GuidRepresentation.Standard),
            ["state"] = "WaitingForEvent",
            ["variables"] = variables ?? [],
            ["createdAt"] = now,
            ["updatedAt"] = now,
        };

        await GetSessionsCollection().InsertOneAsync(document);
    }

    public async Task StartAsync()
    {
        await StartAsync(null, null, delayResumeEnabled: true, delayResumeBatchSize: 20);
    }

    public async Task<Exception> StartInvalidContainerAsync(string key, string value)
    {
        if (_scenarioEngineImage is null || _network is null)
            throw new InvalidOperationException("Fixture is not started.");

        var invalidPort = TestPorts.GetFreeTcpPort();
        var invalidContainer = CreateContainerBuilder("invalid", invalidPort, delayResumeEnabled: true)
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
                throw new InvalidOperationException($"Invalid scenario-engine container became ready with '{key}'.");

            return new InvalidOperationException("Invalid scenario-engine container did not become ready.");
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

    public async Task WaitForLockedSessionAsync(Guid botId, string platformUserId)
    {
        await WaitUntilAsync(
            async () =>
            {
                var session = await FindSessionAsync(botId, platformUserId);
                return session is not null && session.Contains("delayResumeLockUntil");
            },
            $"Expected delay session for bot '{botId}' and user '{platformUserId}' to be locked.");
    }

    public async Task WaitForSessionWithoutVariableAsync(Guid botId, string platformUserId, string variableName)
    {
        await WaitUntilAsync(
            async () =>
            {
                var session = await FindSessionAsync(botId, platformUserId);
                return session is not null
                    && !session.GetValue("variables", new BsonDocument()).AsBsonDocument.Contains(variableName);
            },
            $"Expected session for bot '{botId}' and user '{platformUserId}' without variable '{variableName}'.");
    }

    public async Task WaitForSessionAsync(Guid botId, string platformUserId)
    {
        await WaitUntilAsync(
            async () => await FindSessionAsync(botId, platformUserId) is not null,
            $"Expected session for bot '{botId}' and user '{platformUserId}'.");
    }

    public async Task WaitForSessionStateAsync(Guid botId, string platformUserId, string state)
    {
        await WaitUntilAsync(
            async () =>
            {
                var session = await FindSessionAsync(botId, platformUserId);
                return session is not null
                    && string.Equals(session.GetValue("state", string.Empty).AsString, state, StringComparison.Ordinal);
            },
            $"Expected session for bot '{botId}' and user '{platformUserId}' to be in state '{state}'.");
    }

    public async Task WaitForSessionVariableAsync(Guid botId, string platformUserId, string variableName, string expectedValue)
    {
        await WaitUntilAsync(
            async () =>
            {
                var session = await FindSessionAsync(botId, platformUserId);
                if (session is null)
                    return false;

                var variables = session.GetValue("variables", new BsonDocument()).AsBsonDocument;
                return variables.TryGetValue(variableName, out var value)
                    && string.Equals(value.ToString(), expectedValue, StringComparison.Ordinal);
            },
            $"Expected session variable '{variableName}' for bot '{botId}' and user '{platformUserId}' to be '{expectedValue}'.");
    }

    private static Guid ReadGuid(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.String => Guid.Parse(value.AsString),
            BsonType.Binary when value.IsGuid => value.AsGuid,
            _ => Guid.Parse(value.ToString() ?? string.Empty),
        };
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, string failureMessage)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (await condition())
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException(failureMessage);
    }

    private async Task BuildImageAsync()
    {
        var scenarioEngineRoot = TestPaths.FindScenarioEngineRoot();
        var scenarioEngineImageName = $"mazy-scenario-engine-it-final:{_sessionId}";

        _scenarioEngineImage = new ImageFromDockerfileBuilder()
            .WithName(scenarioEngineImageName)
            .WithDockerfileDirectory(scenarioEngineRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "final")
            .Build();

        await _scenarioEngineImage.CreateAsync();
    }

    private ContainerBuilder CreateContainerBuilder(string suffix, int metricsPort, bool delayResumeEnabled)
    {
        return CreateContainerBuilder(suffix, metricsPort, delayResumeEnabled, delayResumeBatchSize: 20);
    }

    private ContainerBuilder CreateContainerBuilder(string suffix, int metricsPort, bool delayResumeEnabled, int delayResumeBatchSize)
    {
        return new ContainerBuilder(_scenarioEngineImage!)
            .WithName($"mazy-scenario-engine-it-{suffix}-{_sessionId}")
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
            .WithEnvironment("MongoDb__ConnectionString", "mongodb://mongodb:27017")
            .WithEnvironment("MongoDb__DatabaseName", MongoDatabaseName)
            .WithEnvironment("DelayResume__Enabled", delayResumeEnabled.ToString(CultureInfo.InvariantCulture))
            .WithEnvironment("DelayResume__PollIntervalSeconds", "1")
            .WithEnvironment("DelayResume__BatchSize", delayResumeBatchSize.ToString(CultureInfo.InvariantCulture))
            .WithEnvironment("DelayResume__LockSeconds", "30")
            .WithEnvironment("Vk__ApiVersion", "5.199")
            .WithEnvironment("BotManager__Address", BotManager.ContainerAddress)
            .WithEnvironment("BotManager__AccessToken", BotManagerAccessToken)
            .WithEnvironment("ScenarioRepository__Address", ScenarioRepository.ContainerAddress);
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

    private IMongoCollection<BsonDocument> GetSessionsCollection()
    {
        if (_mongoClient is null)
            throw new InvalidOperationException("MongoDB is not started.");

        return _mongoClient
            .GetDatabase(MongoDatabaseName)
            .GetCollection<BsonDocument>(SessionsCollectionName);
    }

    private async Task StartAsync(
        Action<FakeBotManagerServer>? configureBotManager,
        Action<FakeScenarioRepositoryServer>? configureScenarioRepository,
        bool delayResumeEnabled,
        int delayResumeBatchSize)
    {
        if (_started)
            return;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _botManager = new FakeBotManagerServer();
        configureBotManager?.Invoke(_botManager);
        await _botManager.StartAsync();

        _scenarioRepository = new FakeScenarioRepositoryServer();
        configureScenarioRepository?.Invoke(_scenarioRepository);
        await _scenarioRepository.StartAsync();

        _network = new NetworkBuilder()
            .WithName($"mazy-scenario-engine-it-{_sessionId}")
            .Build();
        await _network.CreateAsync();

        _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
            .WithName($"mazy-scenario-engine-it-rabbitmq-{_sessionId}")
            .WithUsername(RabbitMqUser)
            .WithPassword(RabbitMqPassword)
            .WithPortBinding(15672, true)
            .WithNetwork(_network)
            .WithNetworkAliases("rabbitmq")
            .Build();

        _mongo = new ContainerBuilder("mongo:8.0")
            .WithName($"mazy-scenario-engine-it-mongo-{_sessionId}")
            .WithPortBinding(27017, true)
            .WithNetwork(_network)
            .WithNetworkAliases("mongodb")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017))
            .Build();

        await _rabbitMq.StartAsync();
        await _mongo.StartAsync();
        await DeclareExchangesAsync();

        _mongoClient = new MongoClient($"mongodb://{_mongo.Hostname}:{_mongo.GetMappedPublicPort(27017)}");

        EventPublisher = new ScenarioEngineEventPublisher(
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
        await StartScenarioEngineContainerAsync(delayResumeEnabled, delayResumeBatchSize);
        _started = true;
    }

    private async Task StartScenarioEngineContainerAsync(bool delayResumeEnabled, int delayResumeBatchSize)
    {
        if (_scenarioEngineImage is null)
            throw new InvalidOperationException("Scenario engine image is not built.");

        _scenarioEngineContainer = CreateContainerBuilder("scenario-engine", _metricsPort, delayResumeEnabled, delayResumeBatchSize)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(_metricsPort, o => o.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        await _scenarioEngineContainer.StartAsync();
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
        foreach (var queue in ScenarioEngineQueues.Main)
        {
            await Queues.WaitForQueueExistsAsync(queue);
            await Queues.WaitForQueueExistsAsync(ScenarioEngineQueues.DeadLetter(queue));
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

        var logs = await GetScenarioEngineLogsAsync();
        throw new TimeoutException(
            $"Scenario engine did not become ready. Last exception: {lastException?.Message}. Logs: {logs}");
    }

    private static class TestPaths
    {
        public static string FindScenarioEngineRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "services", "scenario", "engine");
                if (File.Exists(Path.Combine(candidate, "Dockerfile")))
                    return candidate;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not find services/scenario/engine from test output directory.");
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
