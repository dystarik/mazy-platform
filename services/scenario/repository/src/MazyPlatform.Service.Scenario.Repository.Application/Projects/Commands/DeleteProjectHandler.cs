namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

internal sealed partial class DeleteProjectHandler(
    ILogger<DeleteProjectHandler> logger,
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteProjectCommand>
{
    public async Task<Result> HandleAsync(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(command.ProjectId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

        if (project is null)
        {
            ProjectNotFound(projectId);
            return Error.NotFound(ErrorCodes.Project.NotFound, $"Проект '{projectId}' не найден.");
        }

        if (project.OwnerAccountId != ownerAccountId)
        {
            return Error.Unauthorized(ErrorCodes.Project.AccessDenied, $"Нет доступа к проекту '{projectId}'.");
        }

        project.Delete(timeProvider.GetUtcNow());
        projectRepository.Delete(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Проект '{ProjectId}' не найден.")]
    private partial void ProjectNotFound(Guid projectId);
    #endregion
}
