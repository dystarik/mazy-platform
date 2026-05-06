namespace MazyPlatform.Service.Scenario.Repository.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Api.Common.Extensions;
using MazyPlatform.Service.Scenario.Repository.Application.Projects.Commands;
using MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

internal sealed class ProjectsGrpcService(ICommandDispatcher commands, IQueryDispatcher queries) : ProjectService.ProjectServiceBase
{
    public override async Task<CreateProjectResponse> CreateProject(CreateProjectRequest request, ServerCallContext context)
    {
        var command = new CreateProjectCommand(context.GetUserAccountId(), request.Name, request.PlatformType.ToDomain());
        var result = await commands.DispatchAsync<CreateProjectCommand, CreateProjectResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new CreateProjectResponse { ProjectId = r.ProjectId.ToString() });
    }

    public override async Task<Empty> DeleteProject(DeleteProjectRequest request, ServerCallContext context)
    {
        var command = new DeleteProjectCommand(request.ProjectId, context.GetUserAccountId());
        var result = await commands.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<GetProjectResponse> GetProject(GetProjectRequest request, ServerCallContext context)
    {
        var query = new GetProjectQuery(request.ProjectId, context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetProjectQuery, GetProjectResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r => new GetProjectResponse
        {
            ProjectId = r.ProjectId.ToString(),
            Name = r.Name,
            PlatformType = r.PlatformType.ToProto(),
        });
    }

    public override async Task<GetProjectListResponse> GetProjectList(Empty request, ServerCallContext context)
    {
        var query = new GetProjectListQuery(context.GetUserAccountId());
        var result = await queries.DispatchAsync<GetProjectListQuery, GetProjectListResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetProjectListResponse();
            response.Items.AddRange(r.Items.Select(i => new ProjectListItem
            {
                ProjectId = i.ProjectId.ToString(),
                Name = i.Name,
                PlatformType = i.PlatformType.ToProto(),
            }));
            return response;
        });
    }
}
