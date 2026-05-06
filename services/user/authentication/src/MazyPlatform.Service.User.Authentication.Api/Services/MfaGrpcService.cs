namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;

internal sealed class MfaGrpcService(ICommandDispatcher commands, IQueryDispatcher queries) : MfaService.MfaServiceBase
{
    public override async Task<AddFactorResponse> AddFactor(AddFactorRequest request, ServerCallContext context)
    {
        return request.Factor switch
        {
            MfaFactorType.Totp => await AddTotp(context),
            MfaFactorType.Email => await AddEmail(context, request.HasEmail ? request.Email : null),
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, $"Тип MFA фактора '{request.Factor}' не поддерживается.")),
        };
    }

    public override async Task<ConfirmFactorResponse> ConfirmFactor(ConfirmFactorRequest request, ServerCallContext context)
    {
        var result = request.ConfirmationCase switch
        {
            ConfirmFactorRequest.ConfirmationOneofCase.Totp => await ConfirmTotp(request, context),
            ConfirmFactorRequest.ConfirmationOneofCase.Email => await ConfirmEmail(request, context),
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, $"Тип подтверждения не поддерживается.")),
        };
        return result.ToGrpcResponse(r => new ConfirmFactorResponse
        {
            BackupCodes = { r.ToProto() },
        });
    }

    public override async Task<Empty> RemoveFactor(RemoveFactorRequest request, ServerCallContext context)
    {
        var command = new RemoveMfaMethodCommand(request.MfaSessionId, context.GetUserAccountId(), request.Factor.ToDomain());
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<GetFactorsResponse> GetFactors(Empty request, ServerCallContext context)
    {
        var query = new GetMfaMethodsQuery(context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetMfaMethodsQuery, GetMfaMethodsResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetFactorsResponse
        {
            Factors = { r.Methods.ToProto() },
        });
    }

    public override async Task<RegenerateBackupCodesResponse> RegenerateBackupCodes(RegenerateBackupCodesRequest request, ServerCallContext context)
    {
        var command = new RegenerateBackupCodesCommand(context.GetUserAccountId(), request.MfaSessionId);
        var result = await commands.DispatchAsync<RegenerateBackupCodesCommand, IReadOnlyCollection<BackupCode>>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new RegenerateBackupCodesResponse
        {
            BackupCodes = { r.ToProto() },
        });
    }

    private async Task<AddFactorResponse> AddTotp(ServerCallContext context)
    {
        var command = new AddTotpCommand(context.GetUserAccountId());
        var result = await commands.DispatchAsync<AddTotpCommand, AddTotpResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new AddFactorResponse
        {
            Totp = new TotpSetup { ProvisioningUri = r.ProvisioningUri },
        });
    }

    private async Task<AddFactorResponse> AddEmail(ServerCallContext context, string? email)
    {
        var command = new AddEmailCommand(context.GetUserAccountId(), email);
        var result = await commands.DispatchAsync<AddEmailCommand, AddEmailResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => r.IsVerificationRequired switch
        {
            true => new AddFactorResponse { Email = new EmailMfaSetup { OtpId = r.OtpId.ToString() } },
            false => new AddFactorResponse { Email = new EmailMfaSetup { Success = new EmailMfaSetupSuccess() { BackupCodes = { r.BackupCodes!.ToProto() } } } },
        });
    }

    private async Task<Result<IReadOnlyCollection<BackupCode>>> ConfirmTotp(ConfirmFactorRequest request, ServerCallContext context)
    {
        var command = new ConfirmTotpCommand(context.GetUserAccountId(), request.Totp.Code);
        return await commands.DispatchAsync<ConfirmTotpCommand, IReadOnlyCollection<BackupCode>>(command, context.CancellationToken);
    }

    private async Task<Result<IReadOnlyCollection<BackupCode>>> ConfirmEmail(ConfirmFactorRequest request, ServerCallContext context)
    {
        var command = new ConfirmEmailCommand(request.Email.OtpId, request.Email.Code);
        return await commands.DispatchAsync<ConfirmEmailCommand, IReadOnlyCollection<BackupCode>>(command, context.CancellationToken);
    }
}
