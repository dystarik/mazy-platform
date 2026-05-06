namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Extensions;

internal sealed class GetNodeCatalogHandler(IPlatformNodeCatalog nodeCatalog) : IQueryHandler<GetNodeCatalogQuery, IReadOnlyList<NodeMeta>>
{
    public Task<Result<IReadOnlyList<NodeMeta>>> HandleAsync(GetNodeCatalogQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(nodeCatalog.GetAllMeta(query.PlatformType.ToScenarioPlatformKey())));
    }
}
