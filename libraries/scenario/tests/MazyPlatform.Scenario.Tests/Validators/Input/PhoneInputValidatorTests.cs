namespace MazyPlatform.Scenario.Tests.Validators.Input;

using MazyPlatform.Scenario.Validators.Input;

public class PhoneInputValidatorTests
{
    [Test]
    public async Task IsValid_Plus7Format_ReturnsTrue()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("+79991234567");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_8Format_ReturnsTrue()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("89991234567");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_RussianFormattedNumbers_ReturnsTrue()
    {
        var validator = new PhoneInputValidator();

        var plusSevenResult = validator.IsValid("+7 (999) 123-45-67");
        var eightResult = validator.IsValid("8 (999) 123-45-67");

        await Assert.That(plusSevenResult).IsTrue();
        await Assert.That(eightResult).IsTrue();
    }

    [Test]
    public async Task IsValid_InternationalFormattedNumbers_ReturnsTrue()
    {
        var validator = new PhoneInputValidator();

        var usResult = validator.IsValid("+1 (555) 123-4567");
        var ukResult = validator.IsValid("+44 20 7946 0958");
        var germanyResult = validator.IsValid("0049 30 123456");

        await Assert.That(usResult).IsTrue();
        await Assert.That(ukResult).IsTrue();
        await Assert.That(germanyResult).IsTrue();
    }

    [Test]
    public async Task IsValid_InvalidPhone_ReturnsFalse()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("12345");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_TooLongPhone_ReturnsFalse()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("+1234567890123456");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithLetters_ReturnsFalse()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("+7 phone 999");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithUnsupportedSeparators_ReturnsFalse()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("+7/999/123");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithPlusNotAtStart_ReturnsFalse()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("7+9991234567");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_TrimsWhitespace()
    {
        var validator = new PhoneInputValidator();

        var result = validator.IsValid("  +7 (999) 123-45-67  ");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_WithIgnoredParameters_StillValidatesCorrectly()
    {
        var validator = new PhoneInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["foo"] = "bar",
            ["pattern"] = "ignored",
        };

        var result = validator.IsValid("+79991234567", parameters);

        await Assert.That(result).IsTrue();
    }
}
