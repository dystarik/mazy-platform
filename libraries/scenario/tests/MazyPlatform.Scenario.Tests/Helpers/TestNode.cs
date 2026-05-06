namespace MazyPlatform.Scenario.Tests.Helpers;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

public sealed class TestNode(
    Guid nodeId,
    string nodeType,
    bool isAwaiting,
    Func<ExecutionContext, Task<NodeResult>> executeFunc)
    : NodeBase(nodeId, nodeType, isAwaiting)
{
    public override Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default) =>
        executeFunc(context);
}
