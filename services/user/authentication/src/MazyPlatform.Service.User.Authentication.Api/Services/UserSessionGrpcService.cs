namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;
using MazyPlatform.Service.User.Authentication.Application.UserSessions.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class UserSessionGrpcService(ICommandDispatcher commands, IQueryDispatcher queries) : UserSessionService.UserSessionServiceBase
{
    public override async Task<RefreshSessionResponse> RefreshSession(RefreshSessionRequest request, ServerCallContext context)
    {
        var command = new RefreshSessionCommand(request.RefreshTokenId, request.RefreshToken);
        var result = await commands.DispatchAsync<RefreshSessionCommand, RefreshSessionResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new RefreshSessionResponse
        {
            Tokens = r.ToProto(),
        });
    }

    public override async Task<Empty> Logout(LogoutRequest request, ServerCallContext context)
    {
        var command = new LogoutCommand(request.RefreshTokenId);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> LogoutAll(LogoutAllRequest request, ServerCallContext context)
    {
        var command = new LogoutAllCommand(context.GetUserAccountId(), context.GetRefreshTokenId(), request.ExcludeCurrentSession);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<GetSessionsResponse> GetSessions(Empty request, ServerCallContext context)
    {
        var query = new GetSessionsQuery(context.GetUserAccountId(), context.GetRefreshTokenId());
        var result = await queries.DispatchAsync<GetSessionsQuery, GetSessionsResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetSessionsResponse
        {
            Sessions =
            {
                r.Sessions.Select(s => new SessionInfo
                {
                    RefreshTokenId = s.RefreshTokenId.ToString(),
                    CreatedAt = Timestamp.FromDateTime(s.CreatedAt),
                    IsCurrent = s.IsCurrent,
                }),
            },
        });
    }
}
