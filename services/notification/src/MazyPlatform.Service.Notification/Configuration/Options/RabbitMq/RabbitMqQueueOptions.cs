namespace MazyPlatform.Service.Notification.Configuration.Options.RabbitMq;

using System.ComponentModel.DataAnnotations;

internal sealed class RabbitMqQueueOptions
{
    [Required]
    public required string QueueName { get; set; }

    [Required]
    public required string RoutingKey { get; set; }

    [Required]
    public required string DeadLetterQueueName { get; set; }
}
