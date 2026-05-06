namespace MazyPlatform.Service.User.Authentication.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Api.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class LinkedProviderGrpcService(ICommandDispatcher commands, IQueryDispatcher querys) : LinkedProviderService.LinkedProviderServiceBase
{
    public override async Task<Empty> LinkProvider(LinkProviderRequest request, ServerCallContext context)
    {
        var command = new LinkProviderCommand(context.GetUserAccountId(), request.Provider.ToDomain(), request.Code);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> UnlinkProvider(UnlinkProviderRequest request, ServerCallContext context)
    {
        var command = new UnlinkProviderCommand(context.GetUserAccountId(), request.Provider.ToDomain());
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<GetLinkedProvidersResponse> GetLinkedProviders(Empty request, ServerCallContext context)
    {
        var command = new GetLinkedProvidersQuery(context.GetUserAccountId());
        var result = await querys.DispatchAsync<GetLinkedProvidersQuery, GetLinkedProvidersResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetLinkedProvidersResponse() { Providers = { r.Providers.Select(p => p.ToProto()) } });
    }
}
