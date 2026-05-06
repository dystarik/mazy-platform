namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class ScenarioReleaseChangedIntegrationEventHandler(
    ILogger<ScenarioReleaseChangedIntegrationEventHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<ScenarioReleaseChangedIntegrationEvent>
{
    public async Task HandleAsync(ScenarioReleaseChangedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var bots = await botInstanceRepository.GetAllByProjectIdAsync(@event.ProjectId, cancellationToken);

        if (bots.Count == 0)
        {
            NoBotsForProject(@event.ProjectId);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var changedCount = 0;

        foreach (var bot in bots)
        {
            if (bot.ProjectId is null)
                continue;

            var result = bot.ChangeScenarioVersion(@event.CurrentVersion, now);
            if (result.IsSuccess)
                changedCount++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        BotsVersionChanged(@event.ProjectId, @event.CurrentVersion, changedCount);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Получено ScenarioReleaseChangedIntegrationEvent для проекта '{ProjectId}', но ботов нет.")]
    private partial void NoBotsForProject(Guid projectId);

    [LoggerMessage(2, LogLevel.Information, "Обновлена версия сценария у {Count} ботов проекта '{ProjectId}' на {Version}.")]
    private partial void BotsVersionChanged(Guid projectId, int version, int count);
    #endregion
}
