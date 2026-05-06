namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class ChangeBotScenarioVersionHandler(
    ILogger<ChangeBotScenarioVersionHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    IScenarioRepositoryClient scenarioRepositoryClient,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeBotScenarioVersionCommand>
{
    public async Task<Result> HandleAsync(ChangeBotScenarioVersionCommand command, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(command.BotInstanceId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var botInstance = await botInstanceRepository.GetByIdAndOwnerAsync(botInstanceId, ownerAccountId, cancellationToken);
        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        if (botInstance.ProjectId is null)
        {
            return Error.Conflict(
                ErrorCodes.BotInstance.CannotChangeVersionWhenUnbound,
                "Невозможно изменить версию сценария у бота без привязки к проекту.");
        }

        var validationResult = await scenarioRepositoryClient.ValidateProjectForBotAsync(
            ownerAccountId,
            botInstance.ProjectId.Value,
            botInstance.PlatformType,
            command.NewScenarioVersion,
            cancellationToken);

        if (validationResult.IsFailure)
            return validationResult.Errors;

        var changeResult = botInstance.ChangeScenarioVersion(command.NewScenarioVersion, timeProvider.GetUtcNow());
        if (changeResult.IsFailure)
            return changeResult.Errors;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotInstanceScenarioVersionChanged(botInstanceId, command.NewScenarioVersion);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);

    [LoggerMessage(2, LogLevel.Information, "Версия сценария бота '{BotInstanceId}' изменена на {ScenarioVersion}.")]
    private partial void BotInstanceScenarioVersionChanged(Guid botInstanceId, int scenarioVersion);
    #endregion
}
