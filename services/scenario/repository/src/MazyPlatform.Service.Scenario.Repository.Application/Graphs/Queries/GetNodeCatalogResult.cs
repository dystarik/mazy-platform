namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Scenario.Abstractions.Nodes;

public sealed record GetNodeCatalogResult(IReadOnlyList<NodeMeta> NodeMeta);
