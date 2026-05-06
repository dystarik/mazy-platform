namespace MazyPlatform.Scenario.Tests.Validators.Input;

using MazyPlatform.Scenario.Validators.Input;

public class EmailInputValidatorTests
{
    [Test]
    public async Task IsValid_ValidEmail_ReturnsTrue()
    {
        var validator = new EmailInputValidator();

        var result = validator.IsValid("user@example.com");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_InvalidEmail_ReturnsFalse()
    {
        var validator = new EmailInputValidator();

        var result = validator.IsValid("not-an-email");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_TrimsWhitespace()
    {
        var validator = new EmailInputValidator();

        var result = validator.IsValid("  user@example.com  ");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_WithIgnoredParameters_StillValidatesCorrectly()
    {
        var validator = new EmailInputValidator();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["min"] = "1",
            ["something"] = "else",
        };

        var result = validator.IsValid("user@example.com", parameters);

        await Assert.That(result).IsTrue();
    }
}
