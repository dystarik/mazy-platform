namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

public sealed class FakeBotManagerServer : IAsyncDisposable
{
    private readonly FakeBotManagerState _state = new();
    private WebApplication? _app;

    public string ContainerAddress { get; private set; } = null!;

    public int GetActiveBotsCallCount => _state.GetActiveBotsCalls.Count;

    public IReadOnlyCollection<GetActiveBotsCall> GetActiveBotsCalls => _state.GetActiveBotsCalls.ToArray();

    public int GetBotCredentialsCallCount => _state.GetBotCredentialsCalls.Count;

    public IReadOnlyCollection<GetBotCredentialsCall> GetBotCredentialsCalls => _state.GetBotCredentialsCalls.ToArray();

    public void AddActiveBot(BotPlatformType platformType, ActiveBotInfo bot)
    {
        _state.ActiveBots.AddOrUpdate(
            platformType,
            _ => [bot],
            (_, existing) =>
            {
                existing.Add(bot);
                return existing;
            });
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    public void SetCredentials(Guid botInstanceId, BotCredentials credentials)
    {
        _state.Credentials[botInstanceId] = credentials;
    }

    public void SetGetCredentialsFailure(StatusCode? statusCode)
    {
        _state.GetCredentialsFailure = statusCode;
    }

    public void SetStreamActiveBotsFailure(StatusCode? statusCode)
    {
        _state.StreamActiveBotsFailure = statusCode;
    }

    public async Task StartAsync()
    {
        var port = TestPorts.GetFreeTcpPort();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(_state);

        _app = builder.Build();
        _app.MapGrpcService<FakeBotInternalGrpcService>();

        await _app.StartAsync();
        ContainerAddress = $"http://host.docker.internal:{port}";
    }

    public async Task WaitForGetActiveBotsCallsAsync(int expectedCount)
    {
        await WaitUntilAsync(
            () => _state.GetActiveBotsCalls.Count >= expectedCount,
            $"Expected at least {expectedCount} GetActiveBots calls.");
    }

    public async Task WaitForGetBotCredentialsCallAsync(Guid botInstanceId, int afterCount)
    {
        await WaitUntilAsync(
            () => _state.GetBotCredentialsCalls.Skip(afterCount).Any(x => x.BotInstanceId == botInstanceId),
            $"Expected GetBotCredentials call for bot '{botInstanceId}'.");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string failureMessage)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (condition())
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException(failureMessage);
    }

    public sealed record GetActiveBotsCall(BotPlatformType PlatformType, string? InternalToken);

    public sealed record GetBotCredentialsCall(Guid? BotInstanceId, string BotInstanceIdText, string? InternalToken);

    private static class TestPorts
    {
        public static int GetFreeTcpPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }

    private sealed class FakeBotInternalGrpcService(FakeBotManagerState state) : BotInternalService.BotInternalServiceBase
    {
        public override async Task GetActiveBots(
            GetActiveBotsRequest request,
            IServerStreamWriter<ActiveBotInfo> responseStream,
            ServerCallContext context)
        {
            var token = GetInternalToken(context);
            EnsureAuthorized(token);
            state.GetActiveBotsCalls.Enqueue(new GetActiveBotsCall(request.PlatformType, token));

            if (state.StreamActiveBotsFailure is { } failure)
                throw new RpcException(new Status(failure, "Configured fake bot-manager stream failure."));

            if (!state.ActiveBots.TryGetValue(request.PlatformType, out var bots))
                return;

            foreach (var bot in bots)
                await responseStream.WriteAsync(bot, context.CancellationToken);
        }

        public override Task<GetBotCredentialsResponse> GetBotCredentials(
            GetBotCredentialsRequest request,
            ServerCallContext context)
        {
            var token = GetInternalToken(context);
            EnsureAuthorized(token);
            var botInstanceId = Guid.TryParse(request.BotInstanceId, out var parsed) ? parsed : (Guid?)null;
            state.GetBotCredentialsCalls.Enqueue(new GetBotCredentialsCall(botInstanceId, request.BotInstanceId, token));

            if (state.GetCredentialsFailure is { } failure)
                throw new RpcException(new Status(failure, "Configured fake bot-manager failure."));

            if (botInstanceId is null || !state.Credentials.TryGetValue(botInstanceId.Value, out var credentials))
                throw new RpcException(new Status(StatusCode.NotFound, "Bot credentials not found."));

            return Task.FromResult(new GetBotCredentialsResponse { Credentials = credentials });
        }

        public override Task<HasBotsOnScenarioVersionResponse> HasBotsOnScenarioVersion(
            HasBotsOnScenarioVersionRequest request,
            ServerCallContext context)
        {
            return Task.FromResult(new HasBotsOnScenarioVersionResponse());
        }

        private static void EnsureAuthorized(string? token)
        {
            if (!string.Equals(token, ScenarioEngineFixture.BotManagerAccessToken, StringComparison.Ordinal))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid internal token."));
        }

        private static string? GetInternalToken(ServerCallContext context) =>
            context.RequestHeaders.FirstOrDefault(x => string.Equals(x.Key, "x-internal-token", StringComparison.Ordinal))?.Value;
    }

    private sealed class FakeBotManagerState
    {
        public ConcurrentDictionary<BotPlatformType, List<ActiveBotInfo>> ActiveBots { get; } = new();

        public ConcurrentDictionary<Guid, BotCredentials> Credentials { get; } = new();

        public ConcurrentQueue<GetActiveBotsCall> GetActiveBotsCalls { get; } = new();

        public ConcurrentQueue<GetBotCredentialsCall> GetBotCredentialsCalls { get; } = new();

        public StatusCode? GetCredentialsFailure { get; set; }

        public StatusCode? StreamActiveBotsFailure { get; set; }
    }
}
