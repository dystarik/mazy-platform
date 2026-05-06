namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class MfaServiceProxy(MfaService.MfaServiceClient client) : MfaService.MfaServiceBase
{
    public override Task<AddFactorResponse> AddFactor(AddFactorRequest request, ServerCallContext context)
        => client.AddFactorAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<ConfirmFactorResponse> ConfirmFactor(ConfirmFactorRequest request, ServerCallContext context)
        => client.ConfirmFactorAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> RemoveFactor(RemoveFactorRequest request, ServerCallContext context)
        => client.RemoveFactorAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetFactorsResponse> GetFactors(Empty request, ServerCallContext context)
        => client.GetFactorsAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<RegenerateBackupCodesResponse> RegenerateBackupCodes(RegenerateBackupCodesRequest request, ServerCallContext context)
        => client.RegenerateBackupCodesAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
