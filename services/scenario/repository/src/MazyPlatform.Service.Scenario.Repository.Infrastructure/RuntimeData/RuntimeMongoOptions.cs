namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.RuntimeData;

using System.ComponentModel.DataAnnotations;

internal sealed class RuntimeMongoOptions
{
    public const string SectionName = "MongoDb";

    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string DatabaseName { get; init; }
}
