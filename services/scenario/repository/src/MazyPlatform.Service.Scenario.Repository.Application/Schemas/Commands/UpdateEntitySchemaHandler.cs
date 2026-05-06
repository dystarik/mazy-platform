namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

internal sealed partial class UpdateEntitySchemaHandler(
    ILogger<UpdateEntitySchemaHandler> logger,
    IEntitySchemaRepository entitySchemaRepository,
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateEntitySchemaCommand>
{
    public async Task<Result> HandleAsync(UpdateEntitySchemaCommand command, CancellationToken cancellationToken = default)
    {
        var schemaId = Guid.Parse(command.SchemaId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var schema = await entitySchemaRepository.GetByIdAsync(schemaId, cancellationToken);

        if (schema is null)
        {
            EntitySchemaNotFound(schemaId);
            return Error.NotFound(ErrorCodes.EntitySchema.NotFound, $"Схема сущности '{schemaId}' не найдена.");
        }

        var project = await projectRepository.GetByIdAsync(schema.ProjectId, cancellationToken);

        if (project is null || project.OwnerAccountId != ownerAccountId)
        {
            return Error.Unauthorized(ErrorCodes.EntitySchema.AccessDenied, $"Нет доступа к схеме сущности '{schemaId}'.");
        }

        schema.AddField(command.Name, command.FieldType, command.IsRequired, command.DefaultValue, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Схема сущности '{SchemaId}' не найдена.")]
    private partial void EntitySchemaNotFound(Guid schemaId);
    #endregion
}
