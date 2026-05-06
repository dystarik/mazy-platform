namespace MazyPlatform.Service.Scenario.Repository.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Api.Common.Extensions;
using MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

internal sealed class ScenarioRepositoryInternalGrpcService(ICommandDispatcher commands) : ScenarioRepositoryInternalService.ScenarioRepositoryInternalServiceBase
{
    public override async Task<Empty> ValidateProjectForBot(ValidateProjectForBotRequest request, ServerCallContext context)
    {
        var query = new ValidateProjectForBotQuery(
            request.ProjectId,
            request.OwnerAccountId,
            request.PlatformType.ToDomain(),
            request.ScenarioVersion);
        var result = await commands.DispatchAsync(query, context.CancellationToken);
        return result.ToGrpcResponse();
    }
}
