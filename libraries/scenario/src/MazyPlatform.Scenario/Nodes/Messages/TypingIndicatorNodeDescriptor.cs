namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла индикатора набора текста.
/// </summary>
public sealed class TypingIndicatorNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "typing_indicator";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } = [];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new TypingIndicatorNode(id, services.GetRequiredService<ITypingIndicatorUseCase>());
}
