namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

internal sealed partial class CreateEntitySchemaHandler(
    ILogger<CreateEntitySchemaHandler> logger,
    IEntitySchemaRepository entitySchemaRepository,
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateEntitySchemaCommand, CreateEntitySchemaResult>
{
    public async Task<Result<CreateEntitySchemaResult>> HandleAsync(CreateEntitySchemaCommand command, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(command.ProjectId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

        if (project is null)
        {
            return Error.NotFound(ErrorCodes.EntitySchema.NotFound, $"Проект '{projectId}' не найден.");
        }

        if (project.OwnerAccountId != ownerAccountId)
        {
            return Error.Unauthorized(ErrorCodes.EntitySchema.AccessDenied, $"Нет доступа к проекту '{projectId}'.");
        }

        var schema = EntitySchema.Create(projectId, command.Name, timeProvider.GetUtcNow());

        entitySchemaRepository.Add(schema);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        EntitySchemaCreated(schema.Id);

        return new CreateEntitySchemaResult(schema.Id);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Схема сущности '{SchemaId}' успешно создана.")]
    private partial void EntitySchemaCreated(Guid schemaId);
    #endregion
}
