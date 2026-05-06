namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Abstractions.Validation;
using MazyPlatform.Scenario.Nodes.Messages;

using NSubstitute;

public class ReceiveMessageNodeDescriptorTests
{
    [Test]
    public async Task Schema_HasMessageTextVariable_NotVariable()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("messageTextVariable");
        await Assert.That(keys).DoesNotContain("variable");
    }

    [Test]
    public async Task Schema_MessageTextVariable_IsRequiredString()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();

        var param = descriptor.Schema.Single(
            p => string.Equals(p.Key, "messageTextVariable", StringComparison.Ordinal));

        await Assert.That(param.IsRequired).IsTrue();
        await Assert.That(param.Type).IsEqualTo(NodeParamType.String);
        await Assert.That(param.Description).IsNotNull();
        await Assert.That(param.Description).IsNotEmpty();
    }

    [Test]
    public async Task Schema_MessageIdVariable_IsOptionalString()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();

        var param = descriptor.Schema.Single(
            p => string.Equals(p.Key, "messageIdVariable", StringComparison.Ordinal));

        await Assert.That(param.IsRequired).IsFalse();
        await Assert.That(param.Type).IsEqualTo(NodeParamType.String);
        await Assert.That(param.Description).IsNotNull();
        await Assert.That(param.Description).IsNotEmpty();
    }

    [Test]
    public async Task Validate_WithValidParameters_ReturnsNoErrors()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            { "messageTextVariable": "userInput" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ReturnsReceiveMessageNode()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            { "messageTextVariable": "userInput" }
            """);
        var services = CreateServiceProvider();

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<ReceiveMessageNode>();
        await Assert.That(node.NodeType).IsEqualTo("receive_message");
    }

    [Test]
    public async Task Create_WithMessageIdVariable_ReturnsReceiveMessageNode()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageTextVariable": "userInput",
              "messageIdVariable": "userMessageId"
            }
            """);
        var services = CreateServiceProvider();

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<ReceiveMessageNode>();
        await Assert.That(node.NodeType).IsEqualTo("receive_message");
    }

    [Test]
    public async Task Schema_HasValidatorType_NotValidation()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("validatorType");
        await Assert.That(keys).DoesNotContain("validation");
    }

    [Test]
    public async Task Schema_ValidationParams_IsObjectWithFields()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();

        var validationParams = descriptor.Schema.Single(
            p => string.Equals(p.Key, "validationParams", StringComparison.Ordinal));

        await Assert.That(validationParams.Type).IsEqualTo(NodeParamType.Object);
        await Assert.That(validationParams.Fields).IsNotNull();
        var fieldKeys = validationParams.Fields!.Select(f => f.Key).ToList();
        await Assert.That(fieldKeys).Contains("pattern");
        await Assert.That(fieldKeys).Contains("min");
        await Assert.That(fieldKeys).Contains("max");
    }

    [Test]
    public async Task Validate_ValidatorTypeWithoutErrorMessage_ReturnsConditionalRequiredError()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageTextVariable": "userInput",
              "validatorType": "phone"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("validatorType");
        await Assert.That(errors[0]).Contains("\"errorMessage\"");
    }

    [Test]
    public async Task Validate_ValidatorTypeWithErrorMessage_ReturnsNoErrors()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageTextVariable": "userInput",
              "validatorType": "phone",
              "errorMessage": "Введите телефон в формате +7..."
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_NoValidatorTypeNoErrorMessage_ReturnsNoErrors()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            { "messageTextVariable": "userInput" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_RegexValidatorWithPatternInValidationParams_ReturnsNoErrors()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageTextVariable": "code",
              "validatorType": "regex",
              "validationParams": { "pattern": "^\\d{6}$" },
              "errorMessage": "Введите 6 цифр."
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_NumberValidatorWithMinMaxInValidationParams_ReturnsNoErrors()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageTextVariable": "age",
              "validatorType": "number",
              "validationParams": { "min": "18", "max": "120" },
              "errorMessage": "Введите число от 18 до 120."
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_UnknownValidatorType_ReturnsEnumError()
    {
        var descriptor = new ReceiveMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageTextVariable": "x",
              "validatorType": "banana",
              "errorMessage": "..."
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("validatorType");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

    private static IServiceProvider CreateServiceProvider()
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IReceiveMessageUseCase))
            .Returns(Substitute.For<IReceiveMessageUseCase>());
        services.GetService(typeof(ISendMessageUseCase))
            .Returns(Substitute.For<ISendMessageUseCase>());
        services.GetService(typeof(IEnumerable<IInputValidator>))
            .Returns(Array.Empty<IInputValidator>());
        return services;
    }
}
