namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging.Grpc;

using global::Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Observability;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed partial class BotManagerClient(
    BotInternalService.BotInternalServiceClient grpcClient,
    IOptions<BotManagerClientOptions> options,
    ILogger<BotManagerClient> logger) : IBotManagerClient
{
    private readonly string _token = options.Value.AccessToken;

    public async Task<bool> HasBotsOnScenarioVersionAsync(
        Guid projectId,
        int scenarioVersion,
        CancellationToken cancellationToken = default)
    {
        var callOptions = new CallOptions(
            headers: new Metadata
            {
                { "x-internal-token", _token },
                { TraceContext.HeaderName, TraceContext.GetOrCreate() },
            },
            cancellationToken: cancellationToken);

        var response = await grpcClient.HasBotsOnScenarioVersionAsync(
            new HasBotsOnScenarioVersionRequest
            {
                ProjectId = projectId.ToString(),
                ScenarioVersion = scenarioVersion,
            },
            callOptions);

        HasBotsChecked(projectId, scenarioVersion, response.HasBots);
        return response.HasBots;
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Debug, "HasBotsOnScenarioVersion: projectId={ProjectId}, version={ScenarioVersion}, hasBots={HasBots}.")]
    private partial void HasBotsChecked(Guid projectId, int scenarioVersion, bool hasBots);
    #endregion
}
