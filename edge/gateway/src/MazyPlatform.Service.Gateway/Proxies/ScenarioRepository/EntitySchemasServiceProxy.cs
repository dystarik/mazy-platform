namespace MazyPlatform.Service.Gateway.Proxies.ScenarioRepository;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class EntitySchemasServiceProxy(EntitySchemaService.EntitySchemaServiceClient client) : EntitySchemaService.EntitySchemaServiceBase
{
    public override Task<CreateEntitySchemaResponse> CreateEntitySchema(CreateEntitySchemaRequest request, ServerCallContext context)
        => client.CreateEntitySchemaAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> DeleteEntitySchema(DeleteEntitySchemaRequest request, ServerCallContext context)
        => client.DeleteEntitySchemaAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> UpdateEntitySchema(UpdateEntitySchemaRequest request, ServerCallContext context)
        => client.UpdateEntitySchemaAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetEntitySchemaResponse> GetEntitySchema(GetEntitySchemaRequest request, ServerCallContext context)
        => client.GetEntitySchemaAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetEntitySchemaListResponse> GetEntitySchemaList(GetEntitySchemaListRequest request, ServerCallContext context)
        => client.GetEntitySchemaListAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
