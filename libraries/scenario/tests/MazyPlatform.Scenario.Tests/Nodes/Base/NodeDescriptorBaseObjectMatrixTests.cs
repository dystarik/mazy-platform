namespace MazyPlatform.Scenario.Tests.Nodes.Base;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

public class NodeDescriptorBaseObjectMatrixTests
{
    private static readonly IReadOnlyList<NodeParamSchema> ButtonFields =
    [
        new("label", NodeParamType.String, IsRequired: true),
        new("payload", NodeParamType.String, IsRequired: true),
        new("color", NodeParamType.String, IsRequired: false),
    ];

    private static readonly IReadOnlyList<NodeParamSchema> MatrixSchema =
    [
        new("buttons", NodeParamType.ObjectMatrix, IsRequired: true, Fields: ButtonFields),
    ];

    [Test]
    public async Task Validate_WithValidGrid_ReturnsNoErrors()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            {
              "buttons": [
                [{"label":"a","payload":"x"}],
                [{"label":"b","payload":"y","color":"primary"}, {"label":"c","payload":"z"}]
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithFlatArrayOfObjects_ReturnsRowTypeError()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": [{"label":"a","payload":"x"}] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0]\"");
        await Assert.That(errors[0]).Contains("неверный тип");
    }

    [Test]
    public async Task Validate_WithNonArrayOuter_ReturnsTopLevelTypeError()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": "not-an-array" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons\"");
        await Assert.That(errors[0]).Contains("двумерный массив объектов");
    }

    [Test]
    public async Task Validate_WithObjectAsOuter_ReturnsTopLevelTypeError()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": {"label":"a","payload":"x"} }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons\"");
        await Assert.That(errors[0]).Contains("двумерный массив объектов");
    }

    [Test]
    public async Task Validate_WithNumberInsteadOfRow_ReturnsRowTypeErrorWithIndexedPath()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": [[{"label":"a","payload":"x"}], 42] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[1]\"");
        await Assert.That(errors[0]).Contains("неверный тип");
    }

    [Test]
    public async Task Validate_WithMissingRequiredField_ReturnsErrorWithItemPath()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": [[{"label":"a","payload":"x"}, {"payload":"y"}]] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0][1]\"");
        await Assert.That(errors[0]).Contains("\"label\"");
    }

    [Test]
    public async Task Validate_WithWrongFieldType_ReturnsErrorWithFieldPath()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": [[{"label": 42, "payload":"x"}]] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0][0].label\"");
        await Assert.That(errors[0]).Contains("строка");
    }

    [Test]
    public async Task Validate_WithEmptyOuterArray_ReturnsEmptyError()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": [] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons\"");
        await Assert.That(errors[0]).Contains("не должен быть пустым");
    }

    [Test]
    public async Task Validate_WithEmptyRow_ReturnsRowEmptyError()
    {
        var descriptor = new TestMatrixDescriptor(MatrixSchema);
        var parameters = ParseJson("""
            { "buttons": [[{"label":"a","payload":"x"}], []] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[1]\"");
        await Assert.That(errors[0]).Contains("не должен быть пустым");
    }

    [Test]
    public async Task EnsureSchemaValid_WithoutFields_Throws()
    {
        IReadOnlyList<NodeParamSchema> schema =
        [
            new("buttons", NodeParamType.ObjectMatrix, IsRequired: true),
        ];

        await Assert.That(() => NodeDescriptorBase.EnsureSchemaValid("test_matrix", schema))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task EnsureSchemaValid_WithEnumValues_Throws()
    {
        IReadOnlyList<NodeParamSchema> schema =
        [
            new(
                "buttons",
                NodeParamType.ObjectMatrix,
                IsRequired: true,
                Fields: ButtonFields,
                EnumValues: ["primary"]),
        ];

        await Assert.That(() => NodeDescriptorBase.EnsureSchemaValid("test_matrix", schema))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task EnsureSchemaValid_WithValidFields_DoesNotThrow()
    {
        await Assert.That(() => NodeDescriptorBase.EnsureSchemaValid("test_matrix", MatrixSchema))
            .ThrowsNothing();
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

    private sealed class TestMatrixDescriptor(IReadOnlyList<NodeParamSchema> schema) : NodeDescriptorBase
    {
        public override string Type => "test_matrix";

        public override IReadOnlyList<NodeParamSchema> Schema { get; } = schema;

        public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
            throw new NotSupportedException();
    }
}
