namespace MazyPlatform.Service.Gateway.Proxies.ScenarioRepository;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class ProjectsServiceProxy(ProjectService.ProjectServiceClient client) : ProjectService.ProjectServiceBase
{
    public override Task<CreateProjectResponse> CreateProject(CreateProjectRequest request, ServerCallContext context)
        => client.CreateProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> DeleteProject(DeleteProjectRequest request, ServerCallContext context)
        => client.DeleteProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetProjectResponse> GetProject(GetProjectRequest request, ServerCallContext context)
        => client.GetProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetProjectListResponse> GetProjectList(Empty request, ServerCallContext context)
        => client.GetProjectListAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
