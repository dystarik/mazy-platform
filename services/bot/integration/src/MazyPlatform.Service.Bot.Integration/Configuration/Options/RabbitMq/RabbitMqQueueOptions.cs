namespace MazyPlatform.Service.Bot.Integration.Configuration.Options.RabbitMq;

using System.ComponentModel.DataAnnotations;

internal sealed class RabbitMqQueueOptions
{
    /// <summary>Имя очереди.</summary>
    [Required]
    public required string QueueName { get; set; }

    /// <summary>Routing key для подписки.</summary>
    [Required]
    public required string RoutingKey { get; set; }

    /// <summary>Имя DLQ-очереди.</summary>
    [Required]
    public required string DeadLetterQueueName { get; set; }
}
