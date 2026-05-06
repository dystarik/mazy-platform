namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class ChangeBotScenarioVersionUpdateModeHandler(
    ILogger<ChangeBotScenarioVersionUpdateModeHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeBotScenarioVersionUpdateModeCommand>
{
    public async Task<Result> HandleAsync(ChangeBotScenarioVersionUpdateModeCommand command, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(command.BotInstanceId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var botInstance = await botInstanceRepository.GetByIdAndOwnerAsync(botInstanceId, ownerAccountId, cancellationToken);
        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        var changeResult = botInstance.ChangeScenarioVersionUpdateMode(
            command.ScenarioVersionUpdateMode,
            timeProvider.GetUtcNow());

        if (changeResult.IsFailure)
            return changeResult.Errors;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotInstanceScenarioVersionUpdateModeChanged(botInstanceId, command.ScenarioVersionUpdateMode.ToString());
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);

    [LoggerMessage(2, LogLevel.Information, "Режим обновления версии сценария бота '{BotInstanceId}' изменён на {ScenarioVersionUpdateMode}.")]
    private partial void BotInstanceScenarioVersionUpdateModeChanged(Guid botInstanceId, string scenarioVersionUpdateMode);
    #endregion
}
