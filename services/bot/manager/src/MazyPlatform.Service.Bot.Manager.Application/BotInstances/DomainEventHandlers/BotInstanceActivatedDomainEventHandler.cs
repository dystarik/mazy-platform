namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

internal sealed partial class BotInstanceActivatedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceActivatedDomainEventHandler> logger) : IDomainEventHandler<BotInstanceActivatedDomainEvent>
{
    public async Task HandleAsync(BotInstanceActivatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var platformType = @event.PlatformType switch
        {
            PlatformType.Vk => Contracts.Bot.PlatformType.Vk,
            PlatformType.Telegram => Contracts.Bot.PlatformType.Telegram,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), $"Unsupported platform type: {@event.PlatformType}"),
        };

        var integrationEvent = new BotInstanceActivatedIntegrationEvent(
            @event.OccurredAt,
            @event.BotInstanceId,
            @event.ProjectId,
            platformType,
            @event.CommunityId,
            @event.ScenarioVersion);

        await publisher.PublishAsync(integrationEvent, cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceActivatedIntegrationEvent для бота '{BotInstanceId}'.")]
    private partial void IntegrationEventPublished(Guid botInstanceId);
    #endregion
}
