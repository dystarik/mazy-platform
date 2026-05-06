namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.Gateway.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

[Authorize]
internal sealed class PasswordServiceProxy(PasswordService.PasswordServiceClient client) : PasswordService.PasswordServiceBase
{
    public override Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
        => client.ChangePasswordAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<SetPasswordResponse> SetPassword(SetPasswordRequest request, ServerCallContext context)
        => client.SetPasswordAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
        => client.ResetPasswordAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    [AllowAnonymous]
    [EnableRateLimiting(GatewayRateLimitPolicies.PublicAuth)]
    public override Task<Empty> ConfirmResetPassword(ConfirmResetPasswordRequest request, ServerCallContext context)
        => client.ConfirmResetPasswordAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
