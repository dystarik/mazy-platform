namespace MazyPlatform.Scenario.Expressions;

using System.Globalization;

/// <summary>
/// Вычислитель простых условных выражений из трёх частей: левый операнд, оператор, правый операнд.
/// Поддерживает операторы: ==, !=, &gt;, &lt;, &gt;=, &lt;=, contains, startsWith.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>
    /// Сравнивает два значения по указанному оператору.
    /// </summary>
    /// <param name="left">Левый операнд (строка с уже подставленными переменными).</param>
    /// <param name="comparisonOperator">
    /// Оператор сравнения: <c>==</c>, <c>!=</c>, <c>&gt;</c>, <c>&lt;</c>,
    /// <c>&gt;=</c>, <c>&lt;=</c>, <c>contains</c>, <c>startsWith</c>.
    /// </param>
    /// <param name="right">Правый операнд (строка с уже подставленными переменными).</param>
    /// <returns>Результат сравнения.</returns>
    /// <exception cref="ArgumentException">Если оператор пустой.</exception>
    /// <exception cref="InvalidOperationException">
    /// Если оператор неизвестен или числовой оператор получил нечисловые операнды.
    /// </exception>
    public static bool Evaluate(string left, string comparisonOperator, string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonOperator);

        return comparisonOperator switch
        {
            "==" => string.Equals(left, right, StringComparison.Ordinal),
            "!=" => !string.Equals(left, right, StringComparison.Ordinal),
            "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "startsWith" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
            ">" or "<" or ">=" or "<=" => CompareNumeric(left, right, comparisonOperator),
            _ => throw new InvalidOperationException($"Неизвестный оператор: \"{comparisonOperator}\"."),
        };
    }

    private static bool CompareNumeric(string left, string right, string comparisonOperator)
    {
        if (!double.TryParse(left, CultureInfo.InvariantCulture, out var leftNum)
            || !double.TryParse(right, CultureInfo.InvariantCulture, out var rightNum))
        {
            throw new InvalidOperationException(
                $"Оператор \"{comparisonOperator}\" требует числовых значений. Получено: \"{left}\" и \"{right}\".");
        }

        return comparisonOperator switch
        {
            ">" => leftNum > rightNum,
            "<" => leftNum < rightNum,
            ">=" => leftNum >= rightNum,
            "<=" => leftNum <= rightNum,
            _ => throw new InvalidOperationException($"Неизвестный числовой оператор: \"{comparisonOperator}\"."),
        };
    }
}
