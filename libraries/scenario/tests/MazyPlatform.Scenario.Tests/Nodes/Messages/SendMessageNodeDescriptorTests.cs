namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Messages;

public class SendMessageNodeDescriptorTests
{
    [Test]
    public async Task Schema_HasMessageIdVariable_NotResultVariable()
    {
        var descriptor = new SendMessageNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("messageIdVariable");
        await Assert.That(keys).DoesNotContain("resultVariable");
    }

    [Test]
    public async Task Schema_MessageIdVariable_HasDescription()
    {
        var descriptor = new SendMessageNodeDescriptor();

        var messageIdVariable = descriptor.Schema.Single(
            p => string.Equals(p.Key, "messageIdVariable", StringComparison.Ordinal));

        await Assert.That(messageIdVariable.Description).IsNotNull();
        await Assert.That(messageIdVariable.Description).IsNotEmpty();
    }

    [Test]
    public async Task Schema_MessageIdVariable_IsOptional()
    {
        var descriptor = new SendMessageNodeDescriptor();

        var messageIdVariable = descriptor.Schema.Single(
            p => string.Equals(p.Key, "messageIdVariable", StringComparison.Ordinal));

        await Assert.That(messageIdVariable.IsRequired).IsFalse();
        await Assert.That(messageIdVariable.Type).IsEqualTo(NodeParamType.String);
    }
}
