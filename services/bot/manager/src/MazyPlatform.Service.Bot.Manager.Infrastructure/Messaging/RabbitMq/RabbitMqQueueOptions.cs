namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.RabbitMq;

using System.ComponentModel.DataAnnotations;

internal sealed class RabbitMqQueueOptions
{
    [Required]
    public required string QueueName { get; init; }

    [Required]
    public required string RoutingKey { get; init; }

    [Required]
    public required string DeadLetterQueueName { get; init; }
}
