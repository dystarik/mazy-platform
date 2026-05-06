namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.IntegrationEventHandlers;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Contracts.Scenario.Repository.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

internal sealed partial class ProjectDeletedIntegrationEventHandler(
    ILogger<ProjectDeletedIntegrationEventHandler> logger,
    IBotInstanceRepository botInstanceRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<ProjectDeletedIntegrationEvent>
{
    public async Task HandleAsync(ProjectDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var bots = await botInstanceRepository.GetAllByProjectIdAsync(@event.ProjectId, cancellationToken);

        if (bots.Count is 0)
        {
            NoBotsForProject(@event.ProjectId);
            return;
        }

        var unboundCount = bots
            .Select(bot => bot.UnbindFromProject(timeProvider.GetUtcNow()))
            .Count(result => result.IsSuccess);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        BotsUnbound(@event.ProjectId, unboundCount);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Получено ProjectDeletedIntegrationEvent для проекта '{ProjectId}', но привязанных ботов нет.")]
    private partial void NoBotsForProject(Guid projectId);

    [LoggerMessage(2, LogLevel.Information, "Отвязано {Count} ботов от удалённого проекта '{ProjectId}'.")]
    private partial void BotsUnbound(Guid projectId, int count);
    #endregion
}
