namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.Gateway.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
internal sealed class UserSessionServiceProxy(UserSessionService.UserSessionServiceClient client) : UserSessionService.UserSessionServiceBase
{
    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<RefreshSessionResponse> RefreshSession(RefreshSessionRequest request, ServerCallContext context)
        => client.RefreshSessionAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> Logout(LogoutRequest request, ServerCallContext context)
        => client.LogoutAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> LogoutAll(LogoutAllRequest request, ServerCallContext context)
        => client.LogoutAllAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetSessionsResponse> GetSessions(Empty request, ServerCallContext context)
        => client.GetSessionsAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
