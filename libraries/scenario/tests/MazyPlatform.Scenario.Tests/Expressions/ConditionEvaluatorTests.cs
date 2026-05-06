namespace MazyPlatform.Scenario.Tests.Expressions;

using MazyPlatform.Scenario.Expressions;

public class ConditionEvaluatorTests
{
    [Test]
    public async Task Evaluate_Equality_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("Иван", "==", "Иван");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_Equality_ReturnsFalse()
    {
        var result = ConditionEvaluator.Evaluate("Иван", "==", "Пётр");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Evaluate_Inequality_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("a", "!=", "b");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_GreaterThan_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("10", ">", "5");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_LessThan_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("3", "<", "7");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_GreaterOrEqual_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("5", ">=", "5");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_LessOrEqual_ReturnsFalse()
    {
        var result = ConditionEvaluator.Evaluate("4", "<=", "3");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Evaluate_NumericWithDecimals_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("3.14", ">", "2.71");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_Contains_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("привет мир", "contains", "мир");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_StartsWith_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("hello world", "startsWith", "hello");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_Contains_CaseInsensitive_ReturnsTrue()
    {
        var result = ConditionEvaluator.Evaluate("Привет", "contains", "привет");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Evaluate_NonNumericComparison_ThrowsException()
    {
        await Assert.That(() => ConditionEvaluator.Evaluate("abc", ">", "def"))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Evaluate_EmptyOperator_ThrowsException()
    {
        await Assert.That(() => ConditionEvaluator.Evaluate("a", string.Empty, "b"))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Evaluate_UnknownOperator_ThrowsException()
    {
        await Assert.That(() => ConditionEvaluator.Evaluate("a", "~=", "b"))
            .ThrowsExactly<InvalidOperationException>();
    }
}
