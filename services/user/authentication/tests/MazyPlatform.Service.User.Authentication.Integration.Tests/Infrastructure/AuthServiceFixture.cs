namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

using Grpc.Core;
using Grpc.Net.Client;

using MazyPlatform.Contracts.User.Grpc.Authentication;

using RabbitMQ.Client;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

public sealed class AuthServiceFixture : IAsyncDisposable
{
    public const string ExchangeName = "mazy.exchange";
    public const string RabbitMqUser = "mazy";
    public const string RabbitMqPassword = "mazy-password";
    public const string PostgreSqlDatabase = "mazy_auth_tests";
    public const string PostgreSqlUser = "mazy";
    public const string PostgreSqlPassword = "mazy-password";
    public const string JwtIssuer = "MazyPlatform.Tests";
    public const string JwtAudience = "MazyPlatform.Tests";
    public const string JwtSecretKey = "test-secret-key-with-at-least-32-characters";
    public const string TotpEncryptionKey = "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=";

    private static readonly Lazy<AuthServiceFixture> Lazy = new(() => new AuthServiceFixture());

    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private INetwork? _network;
    private PostgreSqlContainer? _postgres;
    private RabbitMqContainer? _rabbitMq;
    private IContainer? _authContainer;
    private IFutureDockerImage? _finalImage;
    private IFutureDockerImage? _migrationImage;
    private GrpcChannel? _channel;
    private HttpClient? _httpClient;
    private bool _started;

    private AuthServiceFixture()
    {
    }

    public static AuthServiceFixture Shared => Lazy.Value;

    public RegistrationService.RegistrationServiceClient Registration { get; private set; } = null!;

    public AuthenticationService.AuthenticationServiceClient Authentication { get; private set; } = null!;

    public PasswordService.PasswordServiceClient Password { get; private set; } = null!;

    public MfaService.MfaServiceClient Mfa { get; private set; } = null!;

    public MfaSessionService.MfaSessionServiceClient MfaSession { get; private set; } = null!;

    public UserSessionService.UserSessionServiceClient UserSession { get; private set; } = null!;

    public LinkedProviderService.LinkedProviderServiceClient LinkedProvider { get; private set; } = null!;

    public RabbitMqEventCapture Events { get; private set; } = null!;

    public TestUserFactory Users { get; private set; } = null!;

    public Uri BaseAddress { get; private set; } = null!;

    private string PostgreSqlContainerConnectionString =>
        $"Host=postgres;Port=5432;Database={PostgreSqlDatabase};Username={PostgreSqlUser};Password={PostgreSqlPassword}";

    public async Task StartAsync()
    {
        if (_started)
            return;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _network = new NetworkBuilder()
            .WithName($"mazy-auth-it-{_sessionId}")
            .Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithName($"mazy-auth-it-postgres-{_sessionId}")
            .WithDatabase(PostgreSqlDatabase)
            .WithUsername(PostgreSqlUser)
            .WithPassword(PostgreSqlPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .Build();

        _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
            .WithName($"mazy-auth-it-rabbitmq-{_sessionId}")
            .WithUsername(RabbitMqUser)
            .WithPassword(RabbitMqPassword)
            .WithNetwork(_network)
            .WithNetworkAliases("rabbitmq")
            .Build();

        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        await DeclareExchangeAsync();

        Events = new RabbitMqEventCapture(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUser,
            RabbitMqPassword,
            ExchangeName);
        await Events.StartAsync();

        await BuildAuthImagesAsync();
        await RunMigrationsAsync();
        await StartAuthContainerAsync();
        await CreateClientsAsync();

        Users = new TestUserFactory(this);
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

        return await _httpClient.SendAsync(request);
    }

    public async Task<RpcException> StartInvalidAuthContainerAsync()
    {
        if (_network is null || _finalImage is null)
            throw new InvalidOperationException("Fixture is not started.");

        var invalidContainer = CreateAuthContainerBuilder(_finalImage, "auth-invalid")
            .WithEnvironment("Jwt__SecretKey", "too-short")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(8081, o => o.WithTimeout(TimeSpan.FromSeconds(10))))
            .Build();

        try
        {
            await invalidContainer.StartAsync();
            throw new InvalidOperationException("Invalid auth container started successfully.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            return new RpcException(new Status(StatusCode.Unavailable, ex.Message));
        }
        finally
        {
            await invalidContainer.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel?.Dispose();
        _httpClient?.Dispose();

        if (Events is not null)
            await Events.DisposeAsync();
        if (_authContainer is not null)
            await _authContainer.DisposeAsync();
        if (_rabbitMq is not null)
            await _rabbitMq.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
    }

    private static string FindAuthRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "services", "user", "authentication");
            if (File.Exists(Path.Combine(candidate, "Dockerfile")))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find services/user/authentication from test output directory.");
    }

    private async Task DeclareExchangeAsync()
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
    }

    private async Task BuildAuthImagesAsync()
    {
        var authRoot = FindAuthRoot();
        var finalImageName = $"mazy-auth-it-final:{_sessionId}";
        var migrationImageName = $"mazy-auth-it-migration:{_sessionId}";

        _finalImage = new ImageFromDockerfileBuilder()
            .WithName(finalImageName)
            .WithDockerfileDirectory(authRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "final")
            .Build();

        _migrationImage = new ImageFromDockerfileBuilder()
            .WithName(migrationImageName)
            .WithDockerfileDirectory(authRoot)
            .WithDockerfile("Dockerfile")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .WithCreateParameterModifier(parameters => parameters.Target = "migration")
            .Build();

        await _finalImage.CreateAsync();
        await _migrationImage.CreateAsync();
    }

    private async Task RunMigrationsAsync()
    {
        if (_migrationImage is null)
            throw new InvalidOperationException("Migration image is not built.");

        var migrationContainer = new ContainerBuilder(_migrationImage)
            .WithName($"mazy-auth-it-migration-{_sessionId}")
            .WithNetwork(_network!)
            .WithEnvironment("ConnectionStrings__DefaultConnection", PostgreSqlContainerConnectionString)
            .WithEnvironment("TotpEncryption__Key", TotpEncryptionKey)
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

    private async Task StartAuthContainerAsync()
    {
        if (_finalImage is null)
            throw new InvalidOperationException("Auth image is not built.");

        _authContainer = CreateAuthContainerBuilder(_finalImage, "auth")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(8081, o => o.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        await _authContainer.StartAsync();
        BaseAddress = new Uri($"http://{_authContainer.Hostname}:{_authContainer.GetMappedPublicPort(8081)}");
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
        })
        {
            BaseAddress = BaseAddress,
            Timeout = TimeSpan.FromSeconds(15),
        };

        await WaitForReadyAsync();
    }

    private ContainerBuilder CreateAuthContainerBuilder(IFutureDockerImage image, string suffix)
    {
        return new ContainerBuilder(image)
            .WithName($"mazy-auth-it-{suffix}-{_sessionId}")
            .WithNetwork(_network!)
            .WithPortBinding(8081, true)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("MetricsPort", "9090")
            .WithEnvironment("ConnectionStrings__DefaultConnection", PostgreSqlContainerConnectionString)
            .WithEnvironment("RabbitMq__Host", "rabbitmq")
            .WithEnvironment("RabbitMq__Port", "5672")
            .WithEnvironment("RabbitMq__Username", RabbitMqUser)
            .WithEnvironment("RabbitMq__Password", RabbitMqPassword)
            .WithEnvironment("RabbitMq__VirtualHost", "/")
            .WithEnvironment("RabbitMq__ExchangeName", ExchangeName)
            .WithEnvironment("Jwt__SecretKey", JwtSecretKey)
            .WithEnvironment("Jwt__Issuer", JwtIssuer)
            .WithEnvironment("Jwt__Audience", JwtAudience)
            .WithEnvironment("Jwt__ExpirationMinutes", "15")
            .WithEnvironment("TotpEncryption__Key", TotpEncryptionKey)
            .WithEnvironment("Totp__Issuer", "MazyPlatform.Tests")
            .WithEnvironment("Totp__VerificationWindowPastSteps", "1")
            .WithEnvironment("Totp__VerificationWindowFutureSteps", "1")
            .WithEnvironment("ExternalProviders__Yandex__ClientId", "test-client")
            .WithEnvironment("ExternalProviders__Yandex__ClientSecret", "test-secret")
            .WithEnvironment("ExternalProviders__Yandex__RedirectUri", "http://localhost/test");
    }

    private async Task CreateClientsAsync()
    {
        var handler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
        };

        _channel = GrpcChannel.ForAddress(BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = handler,
        });

        Registration = new RegistrationService.RegistrationServiceClient(_channel);
        Authentication = new AuthenticationService.AuthenticationServiceClient(_channel);
        Password = new PasswordService.PasswordServiceClient(_channel);
        Mfa = new MfaService.MfaServiceClient(_channel);
        MfaSession = new MfaSessionService.MfaSessionServiceClient(_channel);
        UserSession = new UserSessionService.UserSessionServiceClient(_channel);
        LinkedProvider = new LinkedProviderService.LinkedProviderServiceClient(_channel);

        await Task.CompletedTask;
    }

    private async Task WaitForReadyAsync()
    {
        if (_httpClient is null)
            throw new InvalidOperationException("HTTP client is not created.");

        var timeoutAt = DateTimeOffset.UtcNow.AddMinutes(2);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                };
                using var response = await _httpClient.SendAsync(request);
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
        if (_authContainer is not null)
        {
            var (stdout, stderr) = await _authContainer.GetLogsAsync();
            logs = stdout + stderr;
        }

        throw new TimeoutException($"Auth service did not become ready. Last exception: {lastException?.Message}. Logs: {logs}");
    }
}
