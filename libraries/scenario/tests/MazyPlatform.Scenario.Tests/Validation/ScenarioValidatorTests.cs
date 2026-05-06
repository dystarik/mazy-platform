namespace MazyPlatform.Scenario.Tests.Validation;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Validation;
using MazyPlatform.Scenario.Vk.Nodes;

public class ScenarioValidatorTests
{
    private static readonly Guid StartNodeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondNodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ThirdNodeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MissingNodeId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Test]
    public async Task Validate_ValidScenario_ReturnsNoErrors()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                {
                  "id": "{{StartNodeId}}",
                  "type": "send_message",
                  "params": { "text": "Привет" }
                },
                {
                  "id": "{{SecondNodeId}}",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "payload", "expectedPayloads": ["menu"], "isEntry": true }
                }
              ],
              "connections": [
                { "from": "{{StartNodeId}}", "to": "{{SecondNodeId}}" }
              ]
            }
            """);

        await Assert.That(validation.IsValid).IsTrue();
        await Assert.That(validation.Errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_InvalidJson_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate("{");

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.invalid_json");
    }

    [Test]
    public async Task Validate_MissingRootFields_ReturnsErrors()
    {
        var validator = CreateValidator();

        var validation = validator.Validate("""{}""");

        await Assert.That(validation.Errors.Count(static e => string.Equals(
            e.Code,
            "scenario.root_field_missing",
            StringComparison.Ordinal))).IsEqualTo(3);
        await Assert.That(validation.Errors.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Validate_DuplicateNodeId_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "send_message", "params": { "text": "one" } },
                { "id": "{{StartNodeId}}", "type": "send_message", "params": { "text": "two" } }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.duplicate_node_id");
    }

    [Test]
    public async Task Validate_UnknownNodeType_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "unknown_node", "params": {} }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.unknown_node_type");
    }

    [Test]
    public async Task Validate_StartNodeIdDoesNotExist_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{MissingNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "send_message", "params": { "text": "Привет" } }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.start_node_not_found");
    }

    [Test]
    public async Task Validate_InvalidConnectionNode_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "send_message", "params": { "text": "Привет" } }
              ],
              "connections": [
                { "from": "{{StartNodeId}}", "to": "{{MissingNodeId}}" }
              ]
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.connection_node_not_found");
    }

    [Test]
    public async Task Validate_DuplicateBranchPerFrom_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "send_message", "params": { "text": "Привет" } },
                { "id": "{{SecondNodeId}}", "type": "send_message", "params": { "text": "one" } },
                { "id": "{{ThirdNodeId}}", "type": "send_message", "params": { "text": "two" } }
              ],
              "connections": [
                { "from": "{{StartNodeId}}", "to": "{{SecondNodeId}}" },
                { "from": "{{StartNodeId}}", "to": "{{ThirdNodeId}}", "branch": "default" }
              ]
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.duplicate_branch");
    }

    [Test]
    public async Task Validate_ConditionWithInvalidBranch_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                {
                  "id": "{{StartNodeId}}",
                  "type": "condition",
                  "params": { "leftOperand": "1", "operator": "==", "rightOperand": "1" }
                },
                { "id": "{{SecondNodeId}}", "type": "send_message", "params": { "text": "ok" } }
              ],
              "connections": [
                { "from": "{{StartNodeId}}", "to": "{{SecondNodeId}}", "branch": "maybe" }
              ]
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.condition_branch_invalid");
    }

    [Test]
    public async Task Validate_SwitchWithUnknownBranch_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                {
                  "id": "{{StartNodeId}}",
                  "type": "switch",
                  "params": {
                    "variable": "selectedAction",
                    "cases": [{ "value": "btn1", "branchKey": "number_branch" }]
                  }
                },
                { "id": "{{SecondNodeId}}", "type": "send_message", "params": { "text": "ok" } }
              ],
              "connections": [
                { "from": "{{StartNodeId}}", "to": "{{SecondNodeId}}", "branch": "text_branch" }
              ]
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.switch_branch_invalid");
    }

    [Test]
    public async Task Validate_OrdinaryNodeWithNonDefaultBranch_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "send_message", "params": { "text": "Привет" } },
                { "id": "{{SecondNodeId}}", "type": "send_message", "params": { "text": "ok" } }
              ],
              "connections": [
                { "from": "{{StartNodeId}}", "to": "{{SecondNodeId}}", "branch": "custom" }
              ]
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.unexpected_branch");
    }

    [Test]
    public async Task Validate_DuplicateEntryPayload_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                {
                  "id": "{{StartNodeId}}",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "payload", "expectedPayloads": ["menu"], "isEntry": true }
                },
                {
                  "id": "{{SecondNodeId}}",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "payload", "expectedPayloads": ["menu"], "isEntry": true }
                }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.duplicate_entry_payload");
    }

    [Test]
    public async Task Validate_DuplicateSwitchCaseValue_ReturnsError()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                {
                  "id": "{{StartNodeId}}",
                  "type": "switch",
                  "params": {
                    "variable": "selectedAction",
                    "cases": [
                      { "value": "btn1", "branchKey": "number_branch" },
                      { "value": "btn1", "branchKey": "text_branch" }
                    ]
                  }
                }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code)).Contains("scenario.duplicate_switch_case_value");
    }

    [Test]
    public async Task Validate_DescriptorValidationErrors_AreReturned()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                { "id": "{{StartNodeId}}", "type": "send_message", "params": {} }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.Errors.Select(static e => e.Code))
            .Contains("scenario.node_params_validation_failed");
        await Assert.That(validation.Errors.Single(static e => string.Equals(
                e.Code,
                "scenario.node_params_validation_failed",
                StringComparison.Ordinal)).NodeId)
            .IsEqualTo(StartNodeId);
    }

    [Test]
    public async Task Validate_VkReplyKeyboardWithOneTimeFalse_ReturnsNoErrors()
    {
        var validator = CreateValidator();

        var validation = validator.Validate($$"""
            {
              "startNodeId": "{{StartNodeId}}",
              "nodes": [
                {
                  "id": "{{StartNodeId}}",
                  "type": "vk_send_keyboard",
                  "params": {
                    "text": "Выберите действие",
                    "buttons": [[{ "label": "Да", "payload": "yes" }]],
                    "oneTime": false
                  }
                }
              ],
              "connections": []
            }
            """);

        await Assert.That(validation.IsValid).IsTrue();
    }

    private static IScenarioValidator CreateValidator()
    {
        INodeSchemaProvider[] providers =
        [
            new SendMessageNodeDescriptor(),
            new ReceiveButtonPressNodeDescriptor(),
            new ConditionNodeDescriptor(),
            new SwitchNodeDescriptor(),
            new VkSendKeyboardNodeDescriptor(),
        ];

        return new ScenarioValidator(providers);
    }
}
