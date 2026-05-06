namespace MazyPlatform.Service.User.Authentication.Domain.Tests.UserAccounts.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class PasswordTests
{
    [Test]
    public async Task Create_Should_ReturnSuccess_When_PasswordIsValid()
    {
        // Arrange
        const string validPassword = "ValidPassword123!";

        // Act
        var result = Password.Create(validPassword);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validPassword);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_PasswordIsTooShort()
    {
        // Arrange
        const string shortPassword = "Short1!";

        // Act
        var result = Password.Create(shortPassword);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_PasswordIsTooLong()
    {
        // Arrange
        var longPassword = new string('A', 130) + "a1!";

        // Act
        var result = Password.Create(longPassword);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_PasswordHasNoUpperCase()
    {
        // Arrange
        const string password = "nouppercase123!";

        // Act
        var result = Password.Create(password);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_PasswordHasNoLowerCase()
    {
        // Arrange
        const string password = "NOLOWERCASE123!";

        // Act
        var result = Password.Create(password);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_PasswordHasNoDigit()
    {
        // Arrange
        const string password = "NoDigitPassword!";

        // Act
        var result = Password.Create(password);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Create_Should_ReturnError_When_PasswordHasNoSpecialChar()
    {
        // Arrange
        const string password = "NoSpecialChar123";

        // Act
        var result = Password.Create(password);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }
}
