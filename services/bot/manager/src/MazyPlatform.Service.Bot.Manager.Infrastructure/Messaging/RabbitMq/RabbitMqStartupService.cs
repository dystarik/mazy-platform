namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.RabbitMq;

using Microsoft.Extensions.Hosting;

internal sealed class RabbitMqStartupService(RabbitMqConnectionFactory factory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) => await factory.GetConnectionAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
