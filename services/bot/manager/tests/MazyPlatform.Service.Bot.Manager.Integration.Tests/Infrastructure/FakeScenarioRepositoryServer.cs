namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Google.Protobuf.WellKnownTypes;

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

    public int ValidateProjectForBotCallCount => _state.ValidateProjectForBotCallCount;

    public void RegisterProject(Guid ownerAccountId, Guid projectId, PlatformType platformType, params int[] versions)
    {
        _state.RegisterProject(ownerAccountId, projectId, platformType, versions);
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
        _app.MapGrpcService<FakeScenarioRepositoryInternalGrpcService>();

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

    private sealed class FakeScenarioRepositoryState
    {
        private readonly ConcurrentDictionary<(Guid OwnerAccountId, Guid ProjectId), FakeProject> _projects = new();
        private int _validateProjectForBotCallCount;

        public int ValidateProjectForBotCallCount => _validateProjectForBotCallCount;

        public void RegisterProject(Guid ownerAccountId, Guid projectId, PlatformType platformType, params int[] versions)
        {
            _projects[(ownerAccountId, projectId)] = new FakeProject(platformType, versions.ToHashSet());
        }

        public void IncrementValidateProjectForBotCallCount()
        {
            Interlocked.Increment(ref _validateProjectForBotCallCount);
        }

        public bool TryGetProject(Guid ownerAccountId, Guid projectId, out FakeProject project) =>
            _projects.TryGetValue((ownerAccountId, projectId), out project!);
    }

    private sealed record FakeProject(PlatformType PlatformType, HashSet<int> Versions);

    private sealed class FakeScenarioRepositoryInternalGrpcService(FakeScenarioRepositoryState state)
        : ScenarioRepositoryInternalService.ScenarioRepositoryInternalServiceBase
    {
        public override Task<Empty> ValidateProjectForBot(ValidateProjectForBotRequest request, ServerCallContext context)
        {
            state.IncrementValidateProjectForBotCallCount();

            var token = context.RequestHeaders.GetValue("x-internal-token");
            if (!string.Equals(token, BotManagerFixture.ScenarioRepositoryAccessToken, StringComparison.Ordinal))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid internal token."));

            if (!Guid.TryParse(request.OwnerAccountId, out var ownerAccountId) ||
                !Guid.TryParse(request.ProjectId, out var projectId))
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Project not found."));
            }

            if (!state.TryGetProject(ownerAccountId, projectId, out var project) ||
                !project.Versions.Contains(request.ScenarioVersion))
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Project or scenario version not found."));
            }

            if (project.PlatformType is not PlatformType.Universal &&
                project.PlatformType != request.PlatformType)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Project platform mismatch."));
            }

            return Task.FromResult(new Empty());
        }
    }
}
