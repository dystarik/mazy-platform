namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: отправить изображение с подписью.
/// </summary>
/// <param name="ImageUrl">URL изображения или file_id платформы.</param>
/// <param name="Caption">Подпись к изображению (опционально).</param>
public sealed record SendImageAction(string ImageUrl, string? Caption = null) : IOutgoingAction;
