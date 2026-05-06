namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.Gateway.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
internal sealed class MfaSessionServiceProxy(MfaSessionService.MfaSessionServiceClient client) : MfaSessionService.MfaSessionServiceBase
{
    public override Task<StartResponse> Start(StartRequest request, ServerCallContext context)
        => client.StartAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetStatusResponse> GetStatus(GetStatusRequest request, ServerCallContext context)
        => client.GetStatusAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> SendEmailCode(SendEmailCodeRequest request, ServerCallContext context)
        => client.SendEmailCodeAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<VerifyCodeResponse> VerifyCode(VerifyCodeRequest request, ServerCallContext context)
        => client.VerifyCodeAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<Empty> SendUnauthenticatedEmailCode(SendUnauthenticatedEmailCodeRequest request, ServerCallContext context)
        => client.SendUnauthenticatedEmailCodeAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<VerifyUnauthenticatedCodeResponse> VerifyUnauthenticatedCode(VerifyUnauthenticatedCodeRequest request, ServerCallContext context)
        => client.VerifyUnauthenticatedCodeAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
