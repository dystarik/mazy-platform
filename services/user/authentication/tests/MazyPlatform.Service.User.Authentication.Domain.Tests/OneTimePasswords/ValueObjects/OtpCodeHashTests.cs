namespace MazyPlatform.Service.User.Authentication.Domain.Tests.OneTimePasswords.ValueObjects;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class OtpCodeHashTests
{
    [Test]
    public async Task Create_Should_ReturnSuccess_When_HashIsValid()
    {
        // Arrange
        const string validHash = "a1b2c3d4e5f6"; // Hex

        // Act
        var result = OtpCodeHash.Create(validHash);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Value).IsEqualTo(validHash);
    }

    [Test]
    public async Task Create_Should_ReturnError_When_HashIsInvalid()
    {
        // Arrange
        const string invalidHash = "not_hex_or_base64_or_bcrypt";

        // Act
        var result = OtpCodeHash.Create(invalidHash);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task FromTrusted_Should_CreateOtpCodeHash_When_Called()
    {
        // Arrange
        const string trustedHash = "any_trusted_value";

        // Act
        var result = OtpCodeHash.FromTrusted(trustedHash);

        // Assert
        await Assert.That(result.Value).IsEqualTo(trustedHash);
    }
}
