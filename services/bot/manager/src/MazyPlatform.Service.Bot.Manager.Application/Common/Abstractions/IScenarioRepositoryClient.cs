namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

public interface IScenarioRepositoryClient
{
    Task<Result> ValidateProjectForBotAsync(
        Guid ownerAccountId,
        Guid projectId,
        PlatformType platformType,
        int? scenarioVersion,
        CancellationToken cancellationToken = default);
}
