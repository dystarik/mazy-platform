namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Commands;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects;

internal sealed partial class CreateProjectHandler(
    ILogger<CreateProjectHandler> logger,
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateProjectCommand, CreateProjectResult>
{
    public async Task<Result<CreateProjectResult>> HandleAsync(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var project = Project.Create(ownerAccountId, command.Name, command.PlatformType, timeProvider.GetUtcNow());

        projectRepository.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ProjectCreated(project.Id);

        return new CreateProjectResult(project.Id);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Проект '{ProjectId}' успешно создан.")]
    private partial void ProjectCreated(Guid projectId);
    #endregion
}
