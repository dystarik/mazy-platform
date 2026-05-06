namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

internal sealed class CreateBotHandler(
    IBotInstanceRepository repository,
    IUnitOfWork unitOfWork,
    IScenarioRepositoryClient scenarioRepositoryClient,
    TimeProvider timeProvider) : ICommandHandler<CreateBotCommand, CreateBotResult>
{
    public async Task<Result<CreateBotResult>> HandleAsync(CreateBotCommand command, CancellationToken cancellationToken = default)
    {
        var ownerAccountId = Guid.Parse(command.OwnerAccountId);
        var projectId = Guid.Parse(command.ProjectId);

        var validationResult = await scenarioRepositoryClient.ValidateProjectForBotAsync(
            ownerAccountId,
            projectId,
            command.PlatformType,
            command.ScenarioVersion,
            cancellationToken);

        if (validationResult.IsFailure)
            return validationResult.Errors;

        var credentialsResult = BuildCredentials(command);
        if (credentialsResult.IsFailure)
            return credentialsResult.Errors;

        var now = timeProvider.GetUtcNow();
        var botInstance = BotInstance.Create(
            ownerAccountId,
            projectId,
            command.Name,
            credentialsResult.Value,
            command.ScenarioVersion,
            now,
            command.ScenarioVersionUpdateMode);

        repository.Add(botInstance);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateBotResult(botInstance.Id);
    }

    private static Result<IBotCredentials> BuildCredentials(CreateBotCommand command) =>
        command.PlatformType switch
        {
            PlatformType.Vk => Result.Success<IBotCredentials>(new VkBotCredentials(command.AccessToken, command.CommunityId!)),
            PlatformType.Telegram => Result.Success<IBotCredentials>(new TelegramBotCredentials(command.AccessToken)),
            _ => (Result<IBotCredentials>)Error.Validation(ErrorCodes.Validation.Invalid, $"Неизвестный тип платформы: {command.PlatformType}"),
        };
}
