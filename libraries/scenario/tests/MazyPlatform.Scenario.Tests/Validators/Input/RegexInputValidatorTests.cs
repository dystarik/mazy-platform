namespace MazyPlatform.Scenario.Tests.Validators.Input;

using MazyPlatform.Scenario.Validators.Input;

public class RegexInputValidatorTests
{
    [Test]
    public async Task IsValid_PatternMatches_ReturnsTrue()
    {
        var validator = new RegexInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pattern"] = @"^\d{3}$",
        };

        var result = validator.IsValid("123", parameters);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_PatternDoesNotMatch_ReturnsFalse()
    {
        var validator = new RegexInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pattern"] = @"^\d{3}$",
        };

        var result = validator.IsValid("abc", parameters);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithoutPattern_ReturnsFalse()
    {
        var validator = new RegexInputValidator();

        var result = validator.IsValid("anything", null);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithEmptyPatternParam_ReturnsFalse()
    {
        var validator = new RegexInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pattern"] = string.Empty,
        };

        var result = validator.IsValid("anything", parameters);

        await Assert.That(result).IsFalse();
    }
}
