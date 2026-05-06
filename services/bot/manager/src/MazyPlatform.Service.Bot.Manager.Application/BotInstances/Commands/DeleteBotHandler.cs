namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class DeleteBotHandler(
    ILogger<DeleteBotHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteBotCommand>
{
    public async Task<Result> HandleAsync(DeleteBotCommand command, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(command.BotInstanceId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var botInstance = await botInstanceRepository.GetByIdAndOwnerAsync(botInstanceId, ownerAccountId, cancellationToken);
        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        botInstance.Delete(timeProvider.GetUtcNow());
        botInstanceRepository.Delete(botInstance);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotInstanceDeleted(botInstanceId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);

    [LoggerMessage(2, LogLevel.Information, "Бот '{BotInstanceId}' успешно удалён.")]
    private partial void BotInstanceDeleted(Guid botInstanceId);
    #endregion
}
