namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
