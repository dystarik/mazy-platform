namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class UnbindBotFromProjectHandler(
    ILogger<UnbindBotFromProjectHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<UnbindBotFromProjectCommand>
{
    public async Task<Result> HandleAsync(UnbindBotFromProjectCommand command, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(command.BotInstanceId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var botInstance = await botInstanceRepository.GetByIdAndOwnerAsync(botInstanceId, ownerAccountId, cancellationToken);
        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        var unbindResult = botInstance.UnbindFromProject(timeProvider.GetUtcNow());
        if (unbindResult.IsFailure)
            return unbindResult.Errors;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotInstanceUnbound(botInstanceId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);

    [LoggerMessage(2, LogLevel.Information, "Бот '{BotInstanceId}' отвязан от проекта.")]
    private partial void BotInstanceUnbound(Guid botInstanceId);
    #endregion
}
