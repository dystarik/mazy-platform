namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

internal sealed class AuthenticationGrpcService(ICommandDispatcher commands) : AuthenticationService.AuthenticationServiceBase
{
    public override async Task<LoginByPasswordResponse> LoginByPassword(LoginByPasswordRequest request, ServerCallContext context)
    {
        var command = new LoginByPasswordCommand(request.Email, request.Password, request.HasMfaSessionId ? request.MfaSessionId : null);
        var result = await commands.DispatchAsync<LoginByPasswordCommand, LoginByPasswordResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            return r.RequiresMfa switch
            {
                true => new LoginByPasswordResponse
                {
                    Challenge = r.MfaRequiredData.ToProto(),
                },
                false => new LoginByPasswordResponse
                {
                    Tokens = r.SuccessData.ToProto(),
                },
            };
        });
    }

    public override async Task<LoginByExternalProviderResponse> LoginByExternalProvider(LoginByExternalProviderRequest request, ServerCallContext context)
    {
        var command = new LoginByExternalProviderCommand(request.Provider.ToDomain(), request.Code);
        var result = await commands.DispatchAsync<LoginByExternalProviderCommand, LoginByExternalProviderResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new LoginByExternalProviderResponse()
        {
            Tokens = new TokenPair()
            {
                AccessToken = r.AccessToken,
                RefreshToken = r.RefreshToken,
            },
        });
    }
}
