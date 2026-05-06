namespace MazyPlatform.Service.User.Authentication.Domain.Tests.Services;

using System;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using NSubstitute;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class CompleteRegistrationServiceTests
{
    [Test]
    public async Task Execute_Should_ReturnSuccess_When_CodeIsValid()
    {
        // Arrange
        var codeHasher = Substitute.For<ICodeHasher>();
        var tokenHasher = Substitute.For<ITokenHasher>();
        var timeProvider = Substitute.For<TimeProvider>();

        var service = new CompleteRegistrationService(codeHasher, tokenHasher, timeProvider);

        var now = DateTimeOffset.UtcNow;
        timeProvider.GetUtcNow().Returns(now);

        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), now);

        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        codeHasher.Verify(code: "123456", hashedCode: "hashed_code").Returns(true);
        tokenHasher.Hash(Arg.Any<string>()).Returns("hashed_token");

        var otp = OneTimePassword.Create(account.Id, OtpType.EmailConfirmation, codeHasher, now);

        // Act
        var result = service.Execute(account, otp, "123456");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var output = result.Value!;
        await Assert.That(output.UserSession).IsNotNull();
        await Assert.That(output.RefreshToken).IsNotNullOrEmpty();
        await Assert.That(account.IsEmailVerified).IsTrue(); // check side effect on account
    }

    [Test]
    public async Task Execute_Should_ReturnError_When_CodeIsInvalid()
    {
        // Arrange
        var codeHasher = Substitute.For<ICodeHasher>();
        var tokenHasher = Substitute.For<ITokenHasher>();
        var timeProvider = Substitute.For<TimeProvider>();

        var service = new CompleteRegistrationService(codeHasher, tokenHasher, timeProvider);

        var now = DateTimeOffset.UtcNow;
        timeProvider.GetUtcNow().Returns(now);

        var email = Email.Create("test@example.com").Value!;
        var account = UserAccount.RegisterByPassword(email, PasswordHash.FromTrusted("hash"), now);

        codeHasher.Hash(Arg.Any<string>()).Returns("hashed_code");
        codeHasher.Verify(code: "wrong", hashedCode: "hashed_code").Returns(false);

        var otp = OneTimePassword.Create(account.Id, OtpType.EmailConfirmation, codeHasher, now);

        // Act
        var result = service.Execute(account, otp, "wrong");

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(account.IsEmailVerified).IsFalse();
    }
}
