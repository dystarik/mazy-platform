namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class EmailTests
{
    [Test]
    public async Task Create_Should_ReturnSuccess_When_EmailIsValid()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var result = Email.Create(validEmail);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validEmail.ToLowerInvariant());
    }

    [Test]
    public async Task Create_Should_ReturnError_When_EmailIsTooShort()
    {
        // Arrange
        var shortEmail = "a@b.";

        // Act
        var result = Email.Create(shortEmail);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_EmailIsTooLong()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com";

        // Act
        var result = Email.Create(longEmail);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_EmailFormatIsInvalid()
    {
        // Arrange
        var invalidEmail = "invalid-email";

        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task FromTrusted_Should_CreateEmail_When_Called()
    {
        // Arrange
        var trustedEmail = "Trusted@Example.com";

        // Act
        var result = Email.FromTrusted(trustedEmail);

        // Assert
        await Assert.That(result.Value).IsEqualTo("trusted@example.com");
    }
}
