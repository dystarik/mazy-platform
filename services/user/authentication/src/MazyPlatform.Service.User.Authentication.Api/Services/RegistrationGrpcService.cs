namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

internal sealed class RegistrationGrpcService(ICommandDispatcher commands) : RegistrationService.RegistrationServiceBase
{
    public override async Task<RegisterByPasswordResponse> RegisterByPassword(RegisterByPasswordRequest request, ServerCallContext context)
    {
        var command = new RegisterByPasswordCommand(request.Email, request.Password);
        var result = await commands.DispatchAsync<RegisterByPasswordCommand, RegisterByPasswordResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new RegisterByPasswordResponse() { OtpId = r.OtpId.ToString() });
    }

    public override async Task<CompleteRegistrationResponse> CompleteRegistration(CompleteRegistrationRequest request, ServerCallContext context)
    {
        var command = new CompleteRegistrationCommand(request.OtpId, request.OtpCode);
        var result = await commands.DispatchAsync<CompleteRegistrationCommand, CompleteRegistrationResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new CompleteRegistrationResponse()
        {
            Tokens = r.ToProto(),
        });
    }

    public override async Task<ResendConfirmationCodeResponse> ResendConfirmationCode(ResendConfirmationCodeRequest request, ServerCallContext context)
    {
        var command = new ResendConfirmationCodeCommand(request.Email);
        var result = await commands.DispatchAsync<ResendConfirmationCodeCommand, ResendConfirmationCodeResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new ResendConfirmationCodeResponse() { OtpId = r.OtpId.ToString() });
    }
}
