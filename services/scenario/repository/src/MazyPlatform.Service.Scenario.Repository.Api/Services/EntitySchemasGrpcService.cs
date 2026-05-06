namespace MazyPlatform.Service.Scenario.Repository.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Api.Common.Extensions;
using MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;
using MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class EntitySchemasGrpcService(ICommandDispatcher commands, IQueryDispatcher queries) : EntitySchemaService.EntitySchemaServiceBase
{
    public override async Task<CreateEntitySchemaResponse> CreateEntitySchema(CreateEntitySchemaRequest request, ServerCallContext context)
    {
        var command = new CreateEntitySchemaCommand(request.ProjectId, context.GetUserAccountId(), request.Name);
        var result = await commands.DispatchAsync<CreateEntitySchemaCommand, CreateEntitySchemaResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new CreateEntitySchemaResponse { SchemaId = r.SchemaId.ToString() });
    }

    public override async Task<Empty> DeleteEntitySchema(DeleteEntitySchemaRequest request, ServerCallContext context)
    {
        var command = new DeleteEntitySchemaCommand(request.SchemaId, context.GetUserAccountId());
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> UpdateEntitySchema(UpdateEntitySchemaRequest request, ServerCallContext context)
    {
        var command = new UpdateEntitySchemaCommand(
            request.SchemaId,
            context.GetUserAccountId(),
            request.Name,
            request.FieldType.ToDomain(),
            request.IsRequired,
            request.HasDefaultValue ? request.DefaultValue : null);

        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<GetEntitySchemaResponse> GetEntitySchema(GetEntitySchemaRequest request, ServerCallContext context)
    {
        var query = new GetEntitySchemaQuery(request.SchemaId, context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetEntitySchemaQuery, GetEntitySchemaResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetEntitySchemaResponse
            {
                SchemaId = r.SchemaId.ToString(),
                Name = r.Name,
            };
            response.Fields.AddRange(r.Fields.Select(f => new EntityFieldItem
            {
                FieldId = f.FieldId.ToString(),
                Name = f.Name,
                FieldType = f.FieldType.ToProto(),
                IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue ?? string.Empty,
            }));
            return response;
        });
    }

    public override async Task<GetEntitySchemaListResponse> GetEntitySchemaList(GetEntitySchemaListRequest request, ServerCallContext context)
    {
        var query = new GetEntitySchemaListQuery(request.ProjectId, context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetEntitySchemaListQuery, GetEntitySchemaListResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetEntitySchemaListResponse();
            response.Items.AddRange(r.Items.Select(i => new EntitySchemaListItem
            {
                SchemaId = i.SchemaId.ToString(),
                Name = i.Name,
            }));
            return response;
        });
    }
}
