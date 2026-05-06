namespace MazyPlatform.Scenario.Helpers;

using MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Общие определения схем кнопок для использования в дескрипторах узлов.
/// Единая точка истины — изменения полей кнопки вносятся здесь и автоматически
/// подхватываются всеми переиспользующими дескрипторами.
/// </summary>
public static class ButtonSchemas
{
    /// <summary>
    /// Кнопка обычного сообщения с кнопками (label + payload).
    /// Используется в SendButtons и EditMessage.
    /// </summary>
    public static IReadOnlyList<NodeParamSchema> MessageButton { get; } =
    [
        new(
            "label",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст на кнопке, видимый пользователю."),
        new(
            "payload",
            NodeParamType.String,
            IsRequired: true,
            Description: "Внутренний идентификатор для обработки нажатия."),
        new(
            "style",
            NodeParamType.Enum,
            IsRequired: false,
            Description: "Семантический стиль кнопки.",
            EnumValues: ["primary", "secondary", "success", "danger"]),
    ];

    /// <summary>
    /// Кнопка карточки карусели — расширяет MessageButton полем link
    /// для перехода по внешней ссылке.
    /// </summary>
    public static IReadOnlyList<NodeParamSchema> CarouselButton { get; } =
    [
        .. MessageButton,
        new(
            "link",
            NodeParamType.String,
            IsRequired: false,
            Description: "Внешняя ссылка для перехода (опционально)."),
    ];
}
