namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

public sealed class FakeScenarioRepositoryServer : IAsyncDisposable
{
    private readonly FakeScenarioRepositoryState _state = new();
    private WebApplication? _app;

    public string ContainerAddress { get; private set; } = null!;

    public int GetScenarioByVersionCallCount => _state.GetScenarioByVersionCalls.Count;

    public IReadOnlyCollection<GetScenarioByVersionCall> GetScenarioByVersionCalls => _state.GetScenarioByVersionCalls.ToArray();

    public void AddScenario(Guid projectId, int version, string graphJson)
    {
        _state.Graphs[(projectId, version)] = graphJson;
    }

    public void ClearScenarios()
    {
        _state.Graphs.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    public void SetFailure(StatusCode? statusCode)
    {
        _state.Failure = statusCode;
    }

    public bool HasGetScenarioByVersionCall(Guid projectId, int version, int afterCount)
    {
        return _state.GetScenarioByVersionCalls
            .Skip(afterCount)
            .Any(x => x.ProjectId == projectId && x.Version == version);
    }

    public int CountGetScenarioByVersionCalls(Guid projectId, int version, int afterCount = 0)
    {
        return _state.GetScenarioByVersionCalls
            .Skip(afterCount)
            .Count(x => x.ProjectId == projectId && x.Version == version);
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
        _app.MapGrpcService<FakeScenarioGraphGrpcService>();

        await _app.StartAsync();
        ContainerAddress = $"http://host.docker.internal:{port}";
    }

    public async Task WaitForGetScenarioByVersionCallAsync(Guid projectId, int version, int afterCount)
    {
        await WaitUntilAsync(
            () => _state.GetScenarioByVersionCalls.Skip(afterCount).Any(x => x.ProjectId == projectId && x.Version == version),
            $"Expected GetScenarioByVersion call for project '{projectId}' version '{version}'.");
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

    public sealed record GetScenarioByVersionCall(Guid? ProjectId, string ProjectIdText, int Version);

    private static class TestPorts
    {
        public static int GetFreeTcpPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }

    private sealed class FakeScenarioGraphGrpcService(FakeScenarioRepositoryState state) : ScenarioGraphService.ScenarioGraphServiceBase
    {
        public override Task<GetScenarioByVersionResponse> GetScenarioByVersion(
            GetScenarioByVersionRequest request,
            ServerCallContext context)
        {
            var projectId = Guid.TryParse(request.ProjectId, out var parsed) ? parsed : (Guid?)null;
            state.GetScenarioByVersionCalls.Enqueue(new GetScenarioByVersionCall(projectId, request.ProjectId, request.Version));

            if (state.Failure is { } failure)
                throw new RpcException(new Status(failure, "Configured fake scenario-repository failure."));

            if (projectId is null || !state.Graphs.TryGetValue((projectId.Value, request.Version), out var graphJson))
                throw new RpcException(new Status(StatusCode.NotFound, "Scenario graph not found."));

            return Task.FromResult(new GetScenarioByVersionResponse
            {
                ScenarioGraphId = Guid.NewGuid().ToString(),
                GraphJson = graphJson,
                Version = request.Version,
            });
        }
    }

    private sealed class FakeScenarioRepositoryState
    {
        public ConcurrentDictionary<(Guid ProjectId, int Version), string> Graphs { get; } = new();

        public ConcurrentQueue<GetScenarioByVersionCall> GetScenarioByVersionCalls { get; } = new();

        public StatusCode? Failure { get; set; }
    }
}
