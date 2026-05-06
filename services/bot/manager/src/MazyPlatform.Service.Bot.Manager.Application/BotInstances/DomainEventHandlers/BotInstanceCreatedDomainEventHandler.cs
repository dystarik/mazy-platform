namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.DomainEventHandlers;

using MazyPlatform.Contracts.Bot.Manager.Events;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

internal sealed partial class BotInstanceCreatedDomainEventHandler(
    IIntegrationEventPublisher publisher,
    ILogger<BotInstanceCreatedDomainEventHandler> logger) : IDomainEventHandler<BotInstanceCreatedDomainEvent>
{
    public async Task HandleAsync(BotInstanceCreatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var platformType = @event.PlatformType switch
        {
            PlatformType.Vk => Contracts.Bot.PlatformType.Vk,
            PlatformType.Telegram => Contracts.Bot.PlatformType.Telegram,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), $"Unsupported platform type: {@event.PlatformType}"),
        };

        await publisher.PublishAsync(
            new BotInstanceCreatedIntegrationEvent(
                @event.OccurredAt,
                @event.BotInstanceId,
                @event.ProjectId,
                @event.OwnerAccountId,
                platformType),
            cancellationToken);

        IntegrationEventPublished(@event.BotInstanceId);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Опубликовано BotInstanceCreatedIntegrationEvent для бота '{BotInstanceId}'.")]
    private partial void IntegrationEventPublished(Guid botInstanceId);
    #endregion
}
