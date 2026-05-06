namespace MazyPlatform.Scenario.Vk.Builder;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Builder;
using MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Сборщик графа сценария для VK.
/// </summary>
public sealed class VkScenarioPlatformBuilder(IServiceProvider serviceProvider, IEnumerable<INodeDescriptor> descriptors)
    : ScenarioPlatformBuilderBase(serviceProvider, descriptors)
{
    private static readonly IReadOnlyDictionary<Type, Type> UseCases = new Dictionary<Type, Type>
    {
        [typeof(ISendMessageUseCase)] = typeof(VkSendMessageUseCase),
        [typeof(ISendButtonsUseCase)] = typeof(VkSendButtonsUseCase),
        [typeof(ISendImageUseCase)] = typeof(VkSendImageUseCase),
        [typeof(IReceiveMessageUseCase)] = typeof(VkReceiveMessageUseCase),
        [typeof(IReceiveButtonPressUseCase)] = typeof(VkReceiveButtonPressUseCase),
        [typeof(IReceiveImageUseCase)] = typeof(VkReceiveImageUseCase),
        [typeof(IEditMessageUseCase)] = typeof(VkEditMessageUseCase),
        [typeof(ITypingIndicatorUseCase)] = typeof(VkTypingIndicatorUseCase),
        [typeof(IGetUserInfoUseCase)] = typeof(VkGetUserInfoUseCase),
        [typeof(IDeleteMessageUseCase)] = typeof(VkDeleteMessageUseCase),
    };

    /// <inheritdoc />
    public override string PlatformKey => ScenarioPlatformKeys.Vk;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<Type, Type> UseCaseTypeMap => UseCases;
}
