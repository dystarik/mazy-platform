namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.Gateway.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
internal sealed class RegistrationServiceProxy(RegistrationService.RegistrationServiceClient client) : RegistrationService.RegistrationServiceBase
{
    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<RegisterByPasswordResponse> RegisterByPassword(RegisterByPasswordRequest request, ServerCallContext context)
        => client.RegisterByPasswordAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<CompleteRegistrationResponse> CompleteRegistration(CompleteRegistrationRequest request, ServerCallContext context)
        => client.CompleteRegistrationAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<ResendConfirmationCodeResponse> ResendConfirmationCode(ResendConfirmationCodeRequest request, ServerCallContext context)
        => client.ResendConfirmationCodeAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
