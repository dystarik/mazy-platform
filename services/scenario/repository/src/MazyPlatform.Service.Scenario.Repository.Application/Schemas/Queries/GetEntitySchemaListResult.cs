namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

public sealed record GetEntitySchemaListResult(IReadOnlyList<GetEntitySchemaListResult.EntitySchemaListItem> Items)
{
    public sealed record EntitySchemaListItem(Guid SchemaId, string Name);
}
