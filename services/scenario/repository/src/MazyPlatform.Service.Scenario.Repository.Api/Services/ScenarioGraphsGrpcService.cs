namespace MazyPlatform.Service.Scenario.Repository.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Service.Scenario.Repository.Api.Common.Extensions;
using MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;
using MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

using ApiNodeParamType = MazyPlatform.Contracts.Scenario.Repository.Grpc.NodeParamType;
using LibNodeParamType = MazyPlatform.Scenario.Abstractions.Nodes.NodeParamType;

internal sealed class ScenarioGraphsGrpcService(ICommandDispatcher commands, IQueryDispatcher queries) : ScenarioGraphService.ScenarioGraphServiceBase
{
    public override async Task<Empty> UpdateScenarioDraft(UpdateScenarioDraftRequest request, ServerCallContext context)
    {
        var command = new UpdateScenarioDraftCommand(request.ProjectId, context.GetUserAccountId(), request.GraphJson);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> PromoteToRelease(PromoteToReleaseRequest request, ServerCallContext context)
    {
        var command = new PromoteToReleaseCommand(request.ProjectId, context.GetUserAccountId());
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> RollbackRelease(RollbackReleaseRequest request, ServerCallContext context)
    {
        var command = new RollbackReleaseCommand(request.ProjectId, context.GetUserAccountId(), request.TargetVersion);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> DeleteScenarioVersion(DeleteScenarioVersionRequest request, ServerCallContext context)
    {
        var command = new DeleteScenarioVersionCommand(request.ProjectId, context.GetUserAccountId(), request.Version);
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<GetScenarioDraftResponse> GetScenarioDraft(GetScenarioDraftRequest request, ServerCallContext context)
    {
        var query = new GetScenarioDraftQuery(request.ProjectId, context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetScenarioDraftQuery, GetScenarioDraftResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetScenarioDraftResponse
        {
            ScenarioGraphId = r.ScenarioGraphId.ToString(),
            GraphJson = r.GraphJson,
            Version = r.Version,
        });
    }

    public override async Task<ValidateScenarioDraftResponse> ValidateScenarioDraft(ValidateScenarioDraftRequest request, ServerCallContext context)
    {
        var query = new ValidateScenarioDraftQuery(request.ProjectId, context.GetUserAccountId());
        var result = await queries.DispatchAsync<ValidateScenarioDraftQuery, ValidateScenarioDraftResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new ValidateScenarioDraftResponse
            {
                IsValid = r.IsValid,
            };

            response.Errors.AddRange(r.Errors.Select(error =>
            {
                var item = new ScenarioValidationErrorItem
                {
                    Code = error.Code,
                    Message = error.Message,
                };

                if (error.NodeId is { } nodeId)
                    item.NodeId = nodeId.ToString();

                if (!string.IsNullOrWhiteSpace(error.Path))
                    item.Path = error.Path;

                return item;
            }));

            return response;
        });
    }

    public override async Task<GetReleasedScenarioResponse> GetReleasedScenario(GetReleasedScenarioRequest request, ServerCallContext context)
    {
        var query = new GetReleasedScenarioQuery(request.ProjectId);
        var result = await queries.DispatchAsync<GetReleasedScenarioQuery, GetReleasedScenarioResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetReleasedScenarioResponse
        {
            ScenarioGraphId = r.ScenarioGraphId.ToString(),
            GraphJson = r.GraphJson,
            Version = r.Version,
        });
    }

    public override async Task<GetScenarioByVersionResponse> GetScenarioByVersion(GetScenarioByVersionRequest request, ServerCallContext context)
    {
        var query = new GetScenarioByVersionQuery(request.ProjectId, request.Version);
        var result = await queries.DispatchAsync<GetScenarioByVersionQuery, GetScenarioByVersionResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetScenarioByVersionResponse
        {
            ScenarioGraphId = r.ScenarioGraphId.ToString(),
            GraphJson = r.GraphJson,
            Version = r.Version,
        });
    }

    public override async Task<GetNodeCatalogResponse> GetNodeCatalog(GetNodeCatalogRequest request, ServerCallContext context)
    {
        var query = new GetNodeCatalogQuery(request.PlatformType.ToDomain());
        var result = await queries.DispatchAsync<GetNodeCatalogQuery, IReadOnlyList<NodeMeta>>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetNodeCatalogResponse();
            response.Nodes.AddRange(r.Select(n =>
            {
                var item = new NodeCatalogItem
                {
                    Type = n.Type,
                };
                item.Schema.AddRange(n.Schema.Select(MapParam));

                return item;
            }));
            return response;
        });

        static NodeParamItem MapParam(NodeParamSchema p)
        {
            var item = new NodeParamItem
            {
                Key = p.Key,
                Type = Map(p.Type),
                IsRequired = p.IsRequired,
                Description = p.Description ?? string.Empty,
            };

            if (p.EnumValues is not null)
                item.EnumValues.AddRange(p.EnumValues);

            if (p.Fields is not null)
                item.Fields.AddRange(p.Fields.Select(MapParam));

            if (p.MaxRows is { } maxRows)
                item.MaxRows = maxRows;

            if (p.MaxItemsPerRow is { } maxItemsPerRow)
                item.MaxItemsPerRow = maxItemsPerRow;

            if (p.MaxItemsTotal is { } maxItemsTotal)
                item.MaxItemsTotal = maxItemsTotal;

            return item;
        }

        static ApiNodeParamType Map(LibNodeParamType t)
        {
            return t switch
            {
                LibNodeParamType.String => ApiNodeParamType.String,
                LibNodeParamType.Int => ApiNodeParamType.Int,
                LibNodeParamType.Bool => ApiNodeParamType.Bool,
                LibNodeParamType.StringDictionary => ApiNodeParamType.StringDictionary,
                LibNodeParamType.StringList => ApiNodeParamType.StringList,
                LibNodeParamType.Object => ApiNodeParamType.Object,
                LibNodeParamType.Enum => ApiNodeParamType.Enum,
                LibNodeParamType.ObjectList => ApiNodeParamType.ObjectList,
                LibNodeParamType.ObjectMatrix => ApiNodeParamType.ObjectMatrix,
                _ => throw new ArgumentOutOfRangeException(nameof(t), $"Unsupported node param type: {t}"),
            };
        }
    }

    public override async Task<GetVersionHistoryResponse> GetVersionHistory(GetVersionHistoryRequest request, ServerCallContext context)
    {
        var query = new GetVersionHistoryQuery(request.ProjectId, context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetVersionHistoryQuery, GetVersionHistoryResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetVersionHistoryResponse();
            response.Versions.AddRange(r.Versions.Select(v => new VersionItem
            {
                Version = v.Version,
                CreatedAt = v.CreatedAt.ToUnixTimeSeconds(),
                IsCurrent = v.IsCurrent,
            }));
            return response;
        });
    }
}
