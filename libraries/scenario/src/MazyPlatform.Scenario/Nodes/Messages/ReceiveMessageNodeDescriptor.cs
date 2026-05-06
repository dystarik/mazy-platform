namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Abstractions.Validation;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла получения сообщения.
/// </summary>
public sealed class ReceiveMessageNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "receive_message";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "messageTextVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для сохранения текста сообщения от пользователя."),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID входящего сообщения пользователя."),
        new(
            "validatorType",
            NodeParamType.Enum,
            IsRequired: false,
            Description: "Тип проверки введённого значения.",
            EnumValues: ["phone", "email", "number", "regex"]),
        new(
            "validationParams",
            NodeParamType.Object,
            IsRequired: false,
            Description: "Параметры выбранного валидатора. Зависят от validatorType.",
            Fields:
            [
                new(
                    "pattern",
                    NodeParamType.String,
                    IsRequired: false,
                    Description: "Регулярное выражение (только для validatorType='regex')."),
                new(
                    "min",
                    NodeParamType.String,
                    IsRequired: false,
                    Description: "Минимум числа в формате с точкой (например, '0' или '1.5'). Только для validatorType='number'."),
                new(
                    "max",
                    NodeParamType.String,
                    IsRequired: false,
                    Description: "Максимум числа в формате с точкой (например, '100' или '99.9'). Только для validatorType='number'."),
            ]),
        new(
            "errorMessage",
            NodeParamType.String,
            IsRequired: false,
            Description: "Сообщение об ошибке при неверном вводе. Обязательно заполнить, если выбран validatorType."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services)
    {
        var validators = services.GetServices<IInputValidator>()
            .ToDictionary(v => v.ValidatorType, StringComparer.Ordinal);

        return new ReceiveMessageNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "messageTextVariable"),
            services.GetRequiredService<IReceiveMessageUseCase>(),
            services.GetRequiredService<ISendMessageUseCase>(),
            validators,
            JsonParamHelper.GetOptionalString(parameters, "validatorType"),
            JsonParamHelper.GetOptionalStringDictionary(parameters, "validationParams"),
            JsonParamHelper.GetOptionalString(parameters, "errorMessage"),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> Validate(Guid nodeId, JsonElement parameters)
    {
        var errors = base.Validate(nodeId, parameters).ToList();

        var hasValidator = parameters.TryGetProperty("validatorType", out var validatorElement)
            && !string.IsNullOrWhiteSpace(validatorElement.GetString());

        if (hasValidator)
        {
            if (!parameters.TryGetProperty("errorMessage", out var errorMsgElement)
                || string.IsNullOrWhiteSpace(errorMsgElement.GetString()))
            {
                errors.Add($"Узел {nodeId}: если указан validatorType, параметр \"errorMessage\" обязателен.");
            }
        }

        return errors;
    }
}
