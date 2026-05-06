namespace MazyPlatform.Scenario.Telegram.Builder;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Builder;
using MazyPlatform.Scenario.Telegram.UseCases;

/// <summary>
/// Сборщик графа сценария для Telegram.
/// </summary>
public sealed class TelegramScenarioPlatformBuilder(IServiceProvider serviceProvider, IEnumerable<INodeDescriptor> descriptors)
    : ScenarioPlatformBuilderBase(serviceProvider, descriptors)
{
    private static readonly IReadOnlyDictionary<Type, Type> UseCases = new Dictionary<Type, Type>
    {
        [typeof(ISendMessageUseCase)] = typeof(TelegramSendMessageUseCase),
        [typeof(ISendButtonsUseCase)] = typeof(TelegramSendButtonsUseCase),
        [typeof(ISendImageUseCase)] = typeof(TelegramSendImageUseCase),
        [typeof(IReceiveMessageUseCase)] = typeof(TelegramReceiveMessageUseCase),
        [typeof(IReceiveButtonPressUseCase)] = typeof(TelegramReceiveButtonPressUseCase),
        [typeof(IReceiveImageUseCase)] = typeof(TelegramReceiveImageUseCase),
        [typeof(IEditMessageUseCase)] = typeof(TelegramEditMessageUseCase),
        [typeof(ITypingIndicatorUseCase)] = typeof(TelegramTypingIndicatorUseCase),
        [typeof(IGetUserInfoUseCase)] = typeof(TelegramGetUserInfoUseCase),
        [typeof(IDeleteMessageUseCase)] = typeof(TelegramDeleteMessageUseCase),
    };

    /// <inheritdoc />
    public override string PlatformKey => ScenarioPlatformKeys.Telegram;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<Type, Type> UseCaseTypeMap => UseCases;
}
