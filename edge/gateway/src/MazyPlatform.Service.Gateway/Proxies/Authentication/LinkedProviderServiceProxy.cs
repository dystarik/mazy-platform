namespace MazyPlatform.Service.Gateway.Proxies.Authentication;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.User.Grpc.Authentication;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class LinkedProviderServiceProxy(LinkedProviderService.LinkedProviderServiceClient client) : LinkedProviderService.LinkedProviderServiceBase
{
    public override Task<Empty> LinkProvider(LinkProviderRequest request, ServerCallContext context)
        => client.LinkProviderAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> UnlinkProvider(UnlinkProviderRequest request, ServerCallContext context)
        => client.UnlinkProviderAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetLinkedProvidersResponse> GetLinkedProviders(Empty request, ServerCallContext context)
        => client.GetLinkedProvidersAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
