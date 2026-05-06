namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetEntitySchemaHandler(
    ILogger<GetEntitySchemaHandler> logger,
    IReadOnlyApplicationDbContext dbContext,
    IProjectRepository projectRepository) : IQueryHandler<GetEntitySchemaQuery, GetEntitySchemaResult>
{
    public async Task<Result<GetEntitySchemaResult>> HandleAsync(GetEntitySchemaQuery query, CancellationToken cancellationToken = default)
    {
        var entityId = Guid.Parse(query.SchemaId);
        var schema = await dbContext.EntitySchemas.SingleOrDefaultAsync(x => x.Id == entityId, cancellationToken);

        if (schema is null)
        {
            EntitySchemaNotFound(entityId);
            return Error.NotFound(ErrorCodes.EntitySchema.NotFound, $"Схема сущности '{query.SchemaId}' не найдена.");
        }

        var ownerAccountId = Guid.Parse(query.OwnerAccountId);
        var project = await projectRepository.GetByIdAsync(schema.ProjectId, cancellationToken);
        if (project is null || project.OwnerAccountId != ownerAccountId)
        {
            EntitySchemaAccessDenied(schema.Id, ownerAccountId);
            return Error.Unauthorized(ErrorCodes.EntitySchema.AccessDenied, $"Нет доступа к схеме сущности '{query.SchemaId}'.");
        }

        var fields = schema.Fields
            .Select(f => new GetEntitySchemaResult.EntityField(f.Id, f.Name, f.FieldType, f.IsRequired, f.DefaultValue))
            .ToList();

        return new GetEntitySchemaResult(schema.Id, schema.Name, fields);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Схема сущности '{SchemaId}' не найдена.")]
    private partial void EntitySchemaNotFound(Guid schemaId);

    [LoggerMessage(2, LogLevel.Warning, "Отказано в доступе к схеме сущности '{SchemaId}'. OwnerAccountId: '{OwnerAccountId}'.")]
    private partial void EntitySchemaAccessDenied(Guid schemaId, Guid ownerAccountId);
    #endregion
}
