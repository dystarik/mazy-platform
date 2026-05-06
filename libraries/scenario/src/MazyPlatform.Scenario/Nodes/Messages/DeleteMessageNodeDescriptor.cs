namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла удаления сообщения.
/// </summary>
public sealed class DeleteMessageNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "delete_message";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной с ID сообщения, которое нужно удалить."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new DeleteMessageNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "messageIdVariable"),
            services.GetRequiredService<IDeleteMessageUseCase>());
}
