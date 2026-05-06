namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;

internal sealed class CreateBotWithoutProjectHandler(
    IBotInstanceRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateBotWithoutProjectCommand, CreateBotResult>
{
    public async Task<Result<CreateBotResult>> HandleAsync(CreateBotWithoutProjectCommand command, CancellationToken cancellationToken = default)
    {
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);

        var credentialsResult = BuildCredentials(command);
        if (credentialsResult.IsFailure)
            return credentialsResult.Errors;

        var botInstance = BotInstance.Create(
            ownerAccountId,
            projectId: null,
            command.Name,
            credentialsResult.Value,
            scenarioVersion: null,
            timeProvider.GetUtcNow(),
            command.ScenarioVersionUpdateMode);

        repository.Add(botInstance);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateBotResult(botInstance.Id);
    }

    private static Result<IBotCredentials> BuildCredentials(CreateBotWithoutProjectCommand command) =>
        command.PlatformType switch
        {
            PlatformType.Vk => Result.Success<IBotCredentials>(new VkBotCredentials(command.AccessToken, command.CommunityId!)),
            PlatformType.Telegram => Result.Success<IBotCredentials>(new TelegramBotCredentials(command.AccessToken)),
            _ => (Result<IBotCredentials>)Error.Validation(ErrorCodes.Validation.Invalid, $"Неизвестный тип платформы: {command.PlatformType}"),
        };
}
