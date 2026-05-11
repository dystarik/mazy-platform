namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Google.Protobuf.WellKnownTypes;

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

    public void SetHasBots(Guid projectId, int version, bool hasBots)
    {
        _state.Set(projectId, version, hasBots);
    }

    public async Task StartAsync()
    {
        var port = GetFreeTcpPort();
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

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class FakeBotManagerState
    {
        private readonly ConcurrentDictionary<(Guid ProjectId, int Version), bool> _hasBots = new();

        public bool HasBots(Guid projectId, int version) =>
            _hasBots.TryGetValue((projectId, version), out var value) && value;

        public void Set(Guid projectId, int version, bool hasBots)
        {
            _hasBots[(projectId, version)] = hasBots;
        }
    }

    private sealed class FakeBotInternalGrpcService(FakeBotManagerState state) : BotInternalService.BotInternalServiceBase
    {
        public override Task<HasBotsOnScenarioVersionResponse> HasBotsOnScenarioVersion(
            HasBotsOnScenarioVersionRequest request,
            ServerCallContext context)
        {
            var hasBots = Guid.TryParse(request.ProjectId, out var projectId) &&
                state.HasBots(projectId, request.ScenarioVersion);

            return Task.FromResult(new HasBotsOnScenarioVersionResponse { HasBots = hasBots });
        }

        public override Task<GetBotCredentialsResponse> GetBotCredentials(
            GetBotCredentialsRequest request,
            ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "Not needed by scenario-repository integration tests."));
        }

        public override Task GetActiveBots(
            GetActiveBotsRequest request,
            IServerStreamWriter<ActiveBotInfo> responseStream,
            ServerCallContext context)
        {
            return Task.CompletedTask;
        }
    }
}
