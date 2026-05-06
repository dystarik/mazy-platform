namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class BindBotToProjectHandler(
    ILogger<BindBotToProjectHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    IScenarioRepositoryClient scenarioRepositoryClient,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<BindBotToProjectCommand>
{
    public async Task<Result> HandleAsync(BindBotToProjectCommand command, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(command.BotInstanceId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);
        var projectId = Guid.Parse(command.ProjectId);

        var botInstance = await botInstanceRepository.GetByIdAndOwnerAsync(botInstanceId, ownerAccountId, cancellationToken);
        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        var validationResult = await scenarioRepositoryClient.ValidateProjectForBotAsync(
            ownerAccountId,
            projectId,
            botInstance.PlatformType,
            command.ScenarioVersion,
            cancellationToken);

        if (validationResult.IsFailure)
            return validationResult.Errors;

        var bindResult = botInstance.BindToProject(
            projectId,
            command.ScenarioVersion,
            timeProvider.GetUtcNow(),
            command.ScenarioVersionUpdateMode);
        if (bindResult.IsFailure)
            return bindResult.Errors;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotInstanceBound(botInstanceId, projectId, command.ScenarioVersion);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);

    [LoggerMessage(2, LogLevel.Information, "Бот '{BotInstanceId}' привязан к проекту '{ProjectId}' с версией сценария {ScenarioVersion}.")]
    private partial void BotInstanceBound(Guid botInstanceId, Guid projectId, int scenarioVersion);
    #endregion
}
