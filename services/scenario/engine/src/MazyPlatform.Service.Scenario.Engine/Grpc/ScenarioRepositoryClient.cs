namespace MazyPlatform.Service.Scenario.Engine.Grpc;

using global::Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Engine.Observability;

internal sealed partial class ScenarioRepositoryClient(
    ScenarioGraphService.ScenarioGraphServiceClient grpcClient,
    ILogger<ScenarioRepositoryClient> logger) : IScenarioRepositoryClient
{
    public async Task<string?> GetScenarioJsonAsync(Guid projectId, int version, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await grpcClient.GetScenarioByVersionAsync(
                new GetScenarioByVersionRequest
                {
                    ProjectId = projectId.ToString(),
                    Version = version,
                },
                headers: new Metadata { { TraceContext.HeaderName, TraceContext.GetOrCreate() } },
                cancellationToken: cancellationToken);

            return response.GraphJson;
        }
        catch (RpcException ex)
        {
            LogGetScenarioFailed(ex, projectId, version);
            return null;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Не удалось загрузить сценарий. ProjectId: {ProjectId}, Version: {Version}.")]
    private partial void LogGetScenarioFailed(Exception exception, Guid projectId, int version);
}
