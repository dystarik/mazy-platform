namespace MazyPlatform.Service.User.Authentication.Domain.Tests.OneTimePasswords;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

using NSubstitute;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class OneTimePasswordTests
{
    [Test]
    public async Task Create_Should_CreateOtpAndRaiseEvent_When_ValidParameters()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var codeHasher = Substitute.For<ICodeHasher>();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        var now = DateTimeOffset.UtcNow;

        // Act
        var otp = OneTimePassword.Create(userAccountId, OtpType.EmailConfirmation, codeHasher, now);

        // Assert
        await Assert.That(otp).IsNotNull();
        await Assert.That(otp.UserAccountId).IsEqualTo(userAccountId);
        await Assert.That(otp.Code.Value).IsEqualTo("hashed_code");
        await Assert.That(otp.Type).IsEqualTo(OtpType.EmailConfirmation);
        await Assert.That(otp.ExpiresAt).IsEqualTo(now.Add(TimeSpan.FromMinutes(15))); // GetLifetime
    }

    [Test]
    public async Task Create_Should_ThrowArgumentException_When_UserAccountIdIsEmpty()
    {
        // Arrange
        var codeHasher = Substitute.For<ICodeHasher>();

        // Act
        void Action() => OneTimePassword.Create(Guid.Empty, OtpType.EmailConfirmation, codeHasher, DateTimeOffset.UtcNow);

        // Assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Test]
    public async Task Verify_Should_ReturnSuccess_When_CodeIsValid()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var codeHasher = Substitute.For<ICodeHasher>();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        codeHasher.Verify(code: "123456", hashedCode: "hashed_code").Returns(returnThis: true);
        var now = DateTimeOffset.UtcNow;
        var otp = OneTimePassword.Create(userAccountId, OtpType.EmailConfirmation, codeHasher, now);

        // Act
        var result = otp.Verify(codeHasher, code: "123456", now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(otp.IsVerified).IsTrue();
        await Assert.That(otp.VerifiedAt).IsNotNull();
        await Assert.That(otp.VerifiedAt).IsEqualTo(now);
    }

    [Test]
    public async Task Verify_Should_ReturnError_When_TooManyAttempts()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var codeHasher = Substitute.For<ICodeHasher>();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        codeHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(returnThis: false);
        var now = DateTimeOffset.UtcNow;
        var otp = OneTimePassword.Create(userAccountId, OtpType.EmailConfirmation, codeHasher, now);

        // Simulate max attempts
        for (int i = 0; i < OneTimePassword.MaxAttempts; i++)
        {
            otp.Verify(codeHasher, code: "wrong", now);
        }

        // Act
        var result = otp.Verify(codeHasher, code: "wrong", now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(otp.FailedAttempts).IsEqualTo(OneTimePassword.MaxAttempts);
    }

    [Test]
    public async Task Verify_Should_ReturnError_When_Expired()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var codeHasher = Substitute.For<ICodeHasher>();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        var now = DateTimeOffset.UtcNow;
        var otp = OneTimePassword.Create(userAccountId, OtpType.EmailConfirmation, codeHasher, now);

        var expiredTime = now.AddMinutes(20);

        // Act
        var result = otp.Verify(codeHasher, code: "code", expiredTime);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Verify_Should_ReturnError_When_CodeIsInvalid()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var codeHasher = Substitute.For<ICodeHasher>();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        codeHasher.Verify(code: "wrong_code", hashedCode: "hashed_code").Returns(returnThis: false);
        var now = DateTimeOffset.UtcNow;
        var otp = OneTimePassword.Create(userAccountId, OtpType.EmailConfirmation, codeHasher, now);

        // Act
        var result = otp.Verify(codeHasher, code: "wrong_code", now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(otp.FailedAttempts).IsEqualTo(1);
    }

    [Test]
    public async Task Invalidate_Should_SetInvalidatedAt_When_Called()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var codeHasher = Substitute.For<ICodeHasher>();
        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        var now = DateTimeOffset.UtcNow;
        var otp = OneTimePassword.Create(userAccountId, OtpType.EmailConfirmation, codeHasher, now);

        // Act
        otp.Invalidate(now);

        // Assert
        await Assert.That(otp.IsInvalidated).IsTrue();
        await Assert.That(otp.InvalidatedAt).IsEqualTo(now);
    }
}
