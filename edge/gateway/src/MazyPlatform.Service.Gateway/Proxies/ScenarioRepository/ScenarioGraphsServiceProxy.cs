namespace MazyPlatform.Service.Gateway.Proxies.ScenarioRepository;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class ScenarioGraphsServiceProxy(ScenarioGraphService.ScenarioGraphServiceClient client) : ScenarioGraphService.ScenarioGraphServiceBase
{
    public override Task<Empty> UpdateScenarioDraft(UpdateScenarioDraftRequest request, ServerCallContext context)
        => client.UpdateScenarioDraftAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> PromoteToRelease(PromoteToReleaseRequest request, ServerCallContext context)
        => client.PromoteToReleaseAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> RollbackRelease(RollbackReleaseRequest request, ServerCallContext context)
        => client.RollbackReleaseAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> DeleteScenarioVersion(DeleteScenarioVersionRequest request, ServerCallContext context)
        => client.DeleteScenarioVersionAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetScenarioDraftResponse> GetScenarioDraft(GetScenarioDraftRequest request, ServerCallContext context)
        => client.GetScenarioDraftAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetReleasedScenarioResponse> GetReleasedScenario(GetReleasedScenarioRequest request, ServerCallContext context)
        => client.GetReleasedScenarioAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetScenarioByVersionResponse> GetScenarioByVersion(GetScenarioByVersionRequest request, ServerCallContext context)
        => client.GetScenarioByVersionAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetNodeCatalogResponse> GetNodeCatalog(GetNodeCatalogRequest request, ServerCallContext context)
        => client.GetNodeCatalogAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetVersionHistoryResponse> GetVersionHistory(GetVersionHistoryRequest request, ServerCallContext context)
        => client.GetVersionHistoryAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
