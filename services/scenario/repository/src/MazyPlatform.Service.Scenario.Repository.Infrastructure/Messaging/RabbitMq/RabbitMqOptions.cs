namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging.RabbitMq;

using System.ComponentModel.DataAnnotations;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required]
    public required string VirtualHost { get; init; }

    [Required]
    public required string ExchangeName { get; init; }

    [Required]
    public required string ExchangeType { get; init; }

    [Required]
    public required string DeadLetterExchangeName { get; init; }

    [Range(1, 65535)]
    public required ushort Prefetch { get; init; }

    [MinLength(1)]
    public required List<RabbitMqQueueOptions> Queues { get; init; }
}
