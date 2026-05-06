namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.Gateway.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
internal sealed class AuthenticationServiceProxy(AuthenticationService.AuthenticationServiceClient client) : AuthenticationService.AuthenticationServiceBase
{
    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<LoginByPasswordResponse> LoginByPassword(LoginByPasswordRequest request, ServerCallContext context)
        => client.LoginByPasswordAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<LoginByExternalProviderResponse> LoginByExternalProvider(LoginByExternalProviderRequest request, ServerCallContext context)
        => client.LoginByExternalProviderAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
