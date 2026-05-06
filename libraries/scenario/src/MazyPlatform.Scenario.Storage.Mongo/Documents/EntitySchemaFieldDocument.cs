namespace MazyPlatform.Scenario.Storage.Mongo.Documents;

internal sealed class EntitySchemaFieldDocument
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public string? DefaultValue { get; set; }

    public IReadOnlyList<string>? EnumValues { get; set; }

    public string? ReferenceEntityName { get; set; }
}
