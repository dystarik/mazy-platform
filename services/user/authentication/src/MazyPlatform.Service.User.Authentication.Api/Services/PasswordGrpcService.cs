namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

internal sealed class PasswordGrpcService(ICommandDispatcher commands) : PasswordService.PasswordServiceBase
{
    public override async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        var command = new ChangePasswordCommand(context.GetUserAccountId(), request.CurrentPassword, request.NewPassword, request.MfaSessionId);
        var result = await commands.DispatchAsync<ChangePasswordCommand, ChangePasswordResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => r.RequiresMfa switch
        {
            true => new ChangePasswordResponse
            {
                Challenge = r.MfaRequiredData.ToProto(),
            },
            false => new ChangePasswordResponse { Success = new Empty() },
        });
    }

    public override async Task<SetPasswordResponse> SetPassword(SetPasswordRequest request, ServerCallContext context)
    {
        var command = new SetPasswordCommand(
            context.GetUserAccountId(),
            request.NewPassword,
            request.HasMfaSessionId ? request.MfaSessionId : null);
        var result = await commands.DispatchAsync<SetPasswordCommand, SetPasswordResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => r.RequiresMfa switch
        {
            true => new SetPasswordResponse
            {
                Challenge = r.MfaRequiredData.ToProto(),
            },
            false => new SetPasswordResponse { Success = new Empty() },
        });
    }

    public override async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
    {
        var command = new ResetPasswordCommand(request.Email);
        var result = await commands.DispatchAsync<ResetPasswordCommand, ResetPasswordResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => r.RequiresMfa switch
        {
            true => new ResetPasswordResponse
            {
                Mfa = r.MfaChallengeData.ToProto(),
            },
            false => new ResetPasswordResponse
            {
                Otp = r.OtpChallengeData.ToProto(),
            },
        });
    }

    public override async Task<Empty> ConfirmResetPassword(ConfirmResetPasswordRequest request, ServerCallContext context)
    {
        var command = request.ConfirmationCase switch
        {
            ConfirmResetPasswordRequest.ConfirmationOneofCase.Mfa =>
                new ConfirmResetPasswordCommand(request.NewPassword, OtpId: null, OtpCode: null, request.Mfa.MfaSessionId),

            ConfirmResetPasswordRequest.ConfirmationOneofCase.Otp =>
                new ConfirmResetPasswordCommand(request.NewPassword, request.Otp.OtpId, request.Otp.Code, MfaSessionId: null),
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "Данный тип подтверждения не поддерживается.")),
        };

        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }
}
