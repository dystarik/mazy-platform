namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

public sealed record GetNodeCatalogQuery(PlatformType PlatformType) : IQuery<IReadOnlyList<NodeMeta>>;
