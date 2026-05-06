namespace MazyPlatform.Scenario.Tests.Abstractions;

using MazyPlatform.Scenario.Abstractions.Nodes;

public class NodeParamSchemaTests
{
    [Test]
    public async Task Description_DefaultsToNull()
    {
        var schema = new NodeParamSchema("key", NodeParamType.String, IsRequired: true);

        await Assert.That(schema.Description).IsNull();
    }

    [Test]
    public async Task Description_PreservedWhenSetByNamedArgument()
    {
        var schema = new NodeParamSchema(
            "key",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст параметра.");

        await Assert.That(schema.Description).IsEqualTo("Текст параметра.");
    }

    [Test]
    public async Task Description_CoexistsWithFields()
    {
        var schema = new NodeParamSchema(
            "buttons",
            NodeParamType.ObjectList,
            IsRequired: true,
            Description: "Список кнопок.",
            Fields:
            [
                new("label", NodeParamType.String, IsRequired: true, Description: "Подпись."),
            ]);

        await Assert.That(schema.Description).IsEqualTo("Список кнопок.");
        await Assert.That(schema.Fields).IsNotNull();
        await Assert.That(schema.Fields!.Count).IsEqualTo(1);
        await Assert.That(schema.Fields[0].Description).IsEqualTo("Подпись.");
    }
}
