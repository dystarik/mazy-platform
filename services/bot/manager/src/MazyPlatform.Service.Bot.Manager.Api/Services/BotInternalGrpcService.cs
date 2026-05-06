namespace MazyPlatform.Service.Bot.Manager.Api.Services;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Api.Common.Mapping;
using MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;
using MazyPlatform.Service.Bot.Manager.Application.BotInstances.StreamingQueries;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class BotInternalGrpcService(
    IStreamingQueryDispatcher streamingDispatcher,
    IQueryDispatcher queryDispatcher) : BotInternalService.BotInternalServiceBase
{
    public override async Task GetActiveBots(GetActiveBotsRequest request, IServerStreamWriter<ActiveBotInfo> responseStream, ServerCallContext context)
    {
        var query = new GetActiveBotsQuery(request.PlatformType.ToDomain());
        await foreach (var item in streamingDispatcher.DispatchAsync<GetActiveBotsQuery, ActiveBotItem>(query, context.CancellationToken))
        {
            await responseStream.WriteAsync(
                new ActiveBotInfo
                {
                    BotInstanceId = item.BotInstanceId.ToString(),
                    ProjectId = item.ProjectId.ToString(),
                    ScenarioVersion = item.ScenarioVersion,
                    Credentials = item.Credentials.ToProto(),
                },
                context.CancellationToken);
        }
    }

    public override async Task<GetBotCredentialsResponse> GetBotCredentials(GetBotCredentialsRequest request, ServerCallContext context)
    {
        var query = new GetBotCredentialsQuery(request.BotInstanceId);
        var result = await queryDispatcher.DispatchAsync<GetBotCredentialsQuery, GetBotCredentialsResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetBotCredentialsResponse
        {
            Credentials = r.Credentials.ToProto(),
        });
    }

    public override async Task<HasBotsOnScenarioVersionResponse> HasBotsOnScenarioVersion(HasBotsOnScenarioVersionRequest request, ServerCallContext context)
    {
        var query = new HasBotsOnScenarioVersionQuery(request.ProjectId, request.ScenarioVersion);
        var result = await queryDispatcher.DispatchAsync<HasBotsOnScenarioVersionQuery, bool>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new HasBotsOnScenarioVersionResponse { HasBots = r });
    }
}
