namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;
using MazyPlatform.Service.User.Authentication.Application.MfaSessions.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class MfaSessionGrpcService(ICommandDispatcher commands, IQueryDispatcher queries) : MfaSessionService.MfaSessionServiceBase
{
    public override async Task<StartResponse> Start(StartRequest request, ServerCallContext context)
    {
        var command = new StartCommand(request.Action.ToDomain(), context.GetUserAccountId());
        var result = await commands.DispatchAsync<StartCommand, StartResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new StartResponse
        {
            MfaSessionId = r.MfaSessionId.ToString(),
            RequiredFactorCount = r.RequiredFactorCount,
            AvailableFactors = { r.AvailableFactors.ToProto() },
        });
    }

    public override async Task<Empty> SendEmailCode(SendEmailCodeRequest request, ServerCallContext context)
    {
        var command = new SendEmailCodeCommand(request.MfaSessionId, context.GetUserAccountId());
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<VerifyCodeResponse> VerifyCode(VerifyCodeRequest request, ServerCallContext context)
    {
        var command = new VerifyCodeCommand(request.MfaSessionId, context.GetUserAccountId(), request.Factor.ToDomain(), request.Code);
        var result = await commands.DispatchAsync<VerifyCodeCommand, VerifyCodeResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new VerifyCodeResponse { IsCompleted = r.IsCompleted });
    }

    public override async Task<GetStatusResponse> GetStatus(GetStatusRequest request, ServerCallContext context)
    {
        var query = new GetStatusQuery(context.GetUserAccountId(), request.MfaSessionId);
        var result = await queries.DispatchAsync<GetStatusQuery, GetStatusResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetStatusResponse
        {
            IsCompleted = r.IsCompleted,
            IsExpired = r.IsExpired,
            AvailableFactors = { r.AvailableFactors.ToProto() },
            CompletedFactors = { r.CompletedFactors.ToProto() },
            RequiredFactorCount = r.RequiredFactorCount,
        });
    }

    public override async Task<Empty> SendUnauthenticatedEmailCode(SendUnauthenticatedEmailCodeRequest request, ServerCallContext context)
    {
        var command = new SendUnauthenticatedEmailCodeCommand(request.MfaSessionId);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<VerifyUnauthenticatedCodeResponse> VerifyUnauthenticatedCode(VerifyUnauthenticatedCodeRequest request, ServerCallContext context)
    {
        var command = new VerifyUnauthenticatedCodeCommand(request.MfaSessionId, request.Factor.ToDomain(), request.Code);
        var result = await commands.DispatchAsync<VerifyUnauthenticatedCodeCommand, VerifyCodeResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new VerifyUnauthenticatedCodeResponse { IsCompleted = r.IsCompleted });
    }
}
