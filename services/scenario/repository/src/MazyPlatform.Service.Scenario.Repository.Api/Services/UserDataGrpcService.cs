namespace MazyPlatform.Service.Scenario.Repository.Api.Services;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Application.UserData.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class UserDataGrpcService(IQueryDispatcher queries) : UserDataService.UserDataServiceBase
{
    public override async Task<GetUserDataRecordsResponse> GetUserDataRecords(
        GetUserDataRecordsRequest request,
        ServerCallContext context)
    {
        var query = new GetUserDataRecordsQuery(
            request.ProjectId,
            context.GetUserAccountId(),
            request.HasSchemaId ? request.SchemaId : null,
            request.HasScenarioVersion ? request.ScenarioVersion : null,
            request.HasBotId ? request.BotId : null,
            request.HasPlatformUserId ? request.PlatformUserId : null,
            request.IncludeArchived,
            request.PageSize,
            request.PageOffset);

        var result = await queries.DispatchAsync<GetUserDataRecordsQuery, GetUserDataRecordsResult>(
            query,
            context.CancellationToken);

        return result.ToGrpcResponse(ToResponse);
    }

    private static GetUserDataRecordsResponse ToResponse(GetUserDataRecordsResult result)
    {
        var response = new GetUserDataRecordsResponse
        {
            TotalCount = result.TotalCount,
        };

        response.Items.AddRange(result.Items.Select(ToRecordItem));

        return response;
    }

    private static UserDataRecordItem ToRecordItem(GetUserDataRecordsResult.UserDataRecordItem item)
    {
        var response = new UserDataRecordItem
        {
            RecordId = item.RecordId.ToString(),
            SchemaId = item.SchemaId.ToString(),
            SchemaSnapshotId = item.SchemaSnapshotId.ToString(),
            SchemaName = item.SchemaName,
            ScenarioVersion = item.ScenarioVersion,
            BotId = item.BotId.ToString(),
            PlatformUserId = item.PlatformUserId,
            IsArchived = item.IsArchived,
            DataJson = item.DataJson,
            CreatedAt = item.CreatedAt.ToUnixTimeSeconds(),
            UpdatedAt = item.UpdatedAt.ToUnixTimeSeconds(),
        };

        if (item.SessionId is { } sessionId)
        {
            response.SessionId = sessionId.ToString();
        }

        return response;
    }
}
