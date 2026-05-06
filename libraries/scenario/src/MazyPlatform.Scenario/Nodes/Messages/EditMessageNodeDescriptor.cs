namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла редактирования сообщения.
/// </summary>
public sealed class EditMessageNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "edit_message";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной с ID сообщения, которое нужно отредактировать."),
        new(
            "newText",
            NodeParamType.String,
            IsRequired: true,
            Description: "Новый текст сообщения."),
        new(
            "buttons",
            NodeParamType.ObjectMatrix,
            IsRequired: false,
            Description: "Новая раскладка кнопок (опционально, заменяет существующие).",
            Fields: ButtonSchemas.MessageButton,
            AllowEmptyCollection: true),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new EditMessageNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "messageIdVariable"),
            JsonParamHelper.GetRequiredString(parameters, "newText"),
            JsonParamHelper.ParseOptionalButtonLayout(parameters),
            services.GetRequiredService<IEditMessageUseCase>());
}
