namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Раскладка кнопок по строкам.
/// </summary>
/// <param name="Rows">Строки кнопок.</param>
public sealed record ButtonLayout(IReadOnlyList<IReadOnlyList<ButtonDefinition>> Rows)
{
    /// <summary>
    /// Пустая раскладка кнопок.
    /// </summary>
    public static ButtonLayout Empty { get; } = new([]);

    /// <summary>
    /// Возвращает все кнопки раскладки в порядке обхода строк.
    /// </summary>
    public IEnumerable<ButtonDefinition> Buttons => Rows.SelectMany(static row => row);
}
