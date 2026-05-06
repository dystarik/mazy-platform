namespace MazyPlatform.Service.Gateway.Proxies.ScenarioRepository;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class UserDataServiceProxy(UserDataService.UserDataServiceClient client) : UserDataService.UserDataServiceBase
{
    public override Task<GetUserDataRecordsResponse> GetUserDataRecords(
        GetUserDataRecordsRequest request,
        ServerCallContext context)
        => client.GetUserDataRecordsAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
