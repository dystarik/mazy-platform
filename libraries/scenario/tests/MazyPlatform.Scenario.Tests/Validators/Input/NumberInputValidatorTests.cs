namespace MazyPlatform.Scenario.Tests.Validators.Input;

using MazyPlatform.Scenario.Validators.Input;

public class NumberInputValidatorTests
{
    [Test]
    public async Task IsValid_ValidNumber_ReturnsTrue()
    {
        var validator = new NumberInputValidator();

        var result = validator.IsValid("42");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_NonNumericString_ReturnsFalse()
    {
        var validator = new NumberInputValidator();

        var result = validator.IsValid("abc");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_LessThanMin_ReturnsFalse()
    {
        var validator = new NumberInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal) { ["min"] = "10" };

        var result = validator.IsValid("5", parameters);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_GreaterThanMax_ReturnsFalse()
    {
        var validator = new NumberInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal) { ["max"] = "100" };

        var result = validator.IsValid("150", parameters);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithinRange_ReturnsTrue()
    {
        var validator = new NumberInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["min"] = "1",
            ["max"] = "100",
        };

        var result = validator.IsValid("50", parameters);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_NonNumericMinMaxParam_IgnoresBound()
    {
        var validator = new NumberInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["min"] = "abc",
            ["max"] = "xyz",
        };

        var result = validator.IsValid("42", parameters);

        await Assert.That(result).IsTrue();
    }
}
