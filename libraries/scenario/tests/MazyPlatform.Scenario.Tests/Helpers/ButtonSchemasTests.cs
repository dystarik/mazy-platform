namespace MazyPlatform.Scenario.Tests.Helpers;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Vk.Nodes;

public class ButtonSchemasTests
{
    [Test]
    public async Task SendButtonsDescriptor_UsesSharedMessageButtonSchema()
    {
        var descriptor = new SendButtonsNodeDescriptor();
        var fields = FindButtonsFields(descriptor.Schema);

        await Assert.That(ReferenceEquals(fields, ButtonSchemas.MessageButton)).IsTrue();
    }

    [Test]
    public async Task EditMessageDescriptor_UsesSharedMessageButtonSchema()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var fields = FindButtonsFields(descriptor.Schema);

        await Assert.That(ReferenceEquals(fields, ButtonSchemas.MessageButton)).IsTrue();
    }

    [Test]
    public async Task VkSendCarouselDescriptor_UsesSharedCarouselButtonSchema()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var cardsParam = descriptor.Schema.Single(
            p => string.Equals(p.Key, "cards", StringComparison.Ordinal));
        var fields = FindButtonsFields(cardsParam.Fields!);

        await Assert.That(ReferenceEquals(fields, ButtonSchemas.CarouselButton)).IsTrue();
    }

    [Test]
    public async Task CarouselButton_IncludesAllMessageButtonFields()
    {
        var carouselKeys = ButtonSchemas.CarouselButton.Select(p => p.Key).ToList();

        foreach (var messageButton in ButtonSchemas.MessageButton)
        {
            await Assert.That(carouselKeys).Contains(messageButton.Key);
        }
    }

    private static IReadOnlyList<NodeParamSchema> FindButtonsFields(IReadOnlyList<NodeParamSchema> schema)
    {
        var buttons = schema.Single(
            p => string.Equals(p.Key, "buttons", StringComparison.Ordinal));
        return buttons.Fields!;
    }
}
