namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

internal sealed partial class UpdateBotTokenHandler(
    ILogger<UpdateBotTokenHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateBotTokenCommand>
{
    public async Task<Result> HandleAsync(UpdateBotTokenCommand command, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(command.BotInstanceId);
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var botInstance = await botInstanceRepository.GetByIdAndOwnerAsync(botInstanceId, ownerAccountId, cancellationToken);
        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        if (botInstance.PlatformType == PlatformType.Vk && string.IsNullOrWhiteSpace(command.CommunityId))
            return Error.Validation(ErrorCodes.Validation.Required, "CommunityId обязателен для платформы VK.");

        IBotCredentials newCredentials = botInstance.PlatformType switch
        {
            PlatformType.Vk => new VkBotCredentials(command.AccessToken, command.CommunityId!),
            PlatformType.Telegram => new TelegramBotCredentials(command.AccessToken),
            _ => throw new InvalidOperationException($"Неподдерживаемая платформа: {botInstance.PlatformType}"),
        };

        botInstance.UpdateCredentials(newCredentials, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotInstanceTokenUpdated(botInstanceId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);

    [LoggerMessage(2, LogLevel.Information, "Токен бота '{BotInstanceId}' успешно обновлён.")]
    private partial void BotInstanceTokenUpdated(Guid botInstanceId);
    #endregion
}
