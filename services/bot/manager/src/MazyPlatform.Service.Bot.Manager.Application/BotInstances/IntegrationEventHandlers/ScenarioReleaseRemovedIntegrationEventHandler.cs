namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

internal sealed partial class ScenarioReleaseRemovedIntegrationEventHandler(
    ILogger<ScenarioReleaseRemovedIntegrationEventHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<ScenarioReleaseRemovedIntegrationEvent>
{
    public async Task HandleAsync(ScenarioReleaseRemovedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var bots = await botInstanceRepository.GetAllByProjectIdAsync(@event.ProjectId, cancellationToken);

        if (bots.Count == 0)
        {
            NoBotsForProject(@event.ProjectId);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var deactivatedCount = 0;

        foreach (var bot in bots)
        {
            if (bot.Status is not BotStatus.Active)
                continue;

            var result = bot.Deactivate(now);
            if (result.IsSuccess)
                deactivatedCount++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        BotsDeactivated(@event.ProjectId, deactivatedCount);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Получено ScenarioReleaseRemovedIntegrationEvent для проекта '{ProjectId}', но ботов нет.")]
    private partial void NoBotsForProject(Guid projectId);

    [LoggerMessage(2, LogLevel.Information, "Деактивировано {Count} ботов проекта '{ProjectId}' из-за удаления всех версий сценария.")]
    private partial void BotsDeactivated(Guid projectId, int count);
    #endregion
}
