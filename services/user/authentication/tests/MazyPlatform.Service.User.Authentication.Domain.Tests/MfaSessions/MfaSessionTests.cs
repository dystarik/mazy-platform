namespace MazyPlatform.Service.User.Authentication.Domain.Tests.MfaSessions;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public class MfaSessionTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create ────────────────────────────────────────────────────────────────
    [Test]
    public async Task Create_Should_ReturnSession_When_ValidParameters()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();

        // Act
        var session = MfaSession.Create(userAccountId, MfaSessionAction.Login, [MfaMethodType.Email], Now);

        // Assert
        await Assert.That(session.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(session.UserAccountId).IsEqualTo(userAccountId);
        await Assert.That(session.Action).IsEqualTo(MfaSessionAction.Login);
        await Assert.That(session.RequiredFactorCount).IsEqualTo(1);
        await Assert.That(session.ExpiresAt).IsEqualTo(Now.AddMinutes(5));
        await Assert.That(session.IsCompleted).IsFalse();
        await Assert.That(session.IsCompletedByBackupCode).IsFalse();
        await Assert.That(session.ValidUntil).IsNull();
    }

    [Test]
    public async Task Create_Should_RequireOneFactor_When_GenerateBackupCodesWithOnlyOneMethod()
    {
        // Act — action requires 2 factors, but only 1 method available → capped to 1
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.GenerateBackupCodes, [MfaMethodType.Email], Now);

        // Assert
        await Assert.That(session.RequiredFactorCount).IsEqualTo(1);
    }

    [Test]
    public async Task Create_Should_RequireTwoFactors_When_GenerateBackupCodesWithTwoMethods()
    {
        // Act — action requires 2 factors and 2 methods available
        var session = MfaSession.Create(
            Guid.NewGuid(),
            MfaSessionAction.GenerateBackupCodes,
            [MfaMethodType.Email, MfaMethodType.Totp],
            Now);

        // Assert
        await Assert.That(session.RequiredFactorCount).IsEqualTo(2);
    }

    // ── CompleteWithFactor ────────────────────────────────────────────────────
    [Test]
    public async Task CompleteWithFactor_Should_ReturnSuccess_And_CompleteSession_When_RequiredFactorProvided()
    {
        // Arrange
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);

        // Act
        var result = session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.IsCompleted).IsTrue();
        await Assert.That(session.CompletedFactors.Count()).IsEqualTo(1);
        await Assert.That(session.ValidUntil).IsEqualTo(Now.AddMinutes(30));
    }

    [Test]
    public async Task CompleteWithFactor_Should_NotComplete_When_FirstOfTwoFactorsProvided()
    {
        // Arrange — 2 factors required
        var session = MfaSession.Create(
            Guid.NewGuid(),
            MfaSessionAction.GenerateBackupCodes,
            [MfaMethodType.Email, MfaMethodType.Totp],
            Now);

        // Act
        var result = session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.IsCompleted).IsFalse();
        await Assert.That(session.ValidUntil).IsNull();
    }

    [Test]
    public async Task CompleteWithFactor_Should_Complete_When_BothFactorsProvided()
    {
        // Arrange — 2 factors required
        var session = MfaSession.Create(
            Guid.NewGuid(),
            MfaSessionAction.GenerateBackupCodes,
            [MfaMethodType.Email, MfaMethodType.Totp],
            Now);
        session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Act
        var result = session.CompleteWithFactor(MfaMethodType.Totp, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.IsCompleted).IsTrue();
        await Assert.That(session.CompletedFactors.Count()).IsEqualTo(2);
        await Assert.That(session.ValidUntil).IsEqualTo(Now.AddMinutes(30));
    }

    [Test]
    public async Task CompleteWithFactor_Should_ReturnError_When_SessionAlreadyCompleted()
    {
        // Arrange
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);
        session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Act
        var result = session.CompleteWithFactor(MfaMethodType.Totp, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.AlreadyCompleted);
    }

    [Test]
    public async Task CompleteWithFactor_Should_ReturnError_When_SessionExpired()
    {
        // Arrange
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);
        var afterExpiry = Now.AddMinutes(6);

        // Act
        var result = session.CompleteWithFactor(MfaMethodType.Email, afterExpiry);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.Expired);
    }

    [Test]
    public async Task CompleteWithFactor_Should_ReturnError_When_FactorAlreadyUsed()
    {
        // Arrange — 2 factors required
        var session = MfaSession.Create(
            Guid.NewGuid(),
            MfaSessionAction.GenerateBackupCodes,
            [MfaMethodType.Email, MfaMethodType.Totp],
            Now);
        session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Act — try to use Email again
        var result = session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.FactorAlreadyUsed);
    }

    // ── CompleteWithBackupCode ────────────────────────────────────────────────
    [Test]
    public async Task CompleteWithBackupCode_Should_ReturnSuccess_And_SetFlag_When_Valid()
    {
        // Arrange
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);

        // Act
        var result = session.CompleteWithBackupCode(Now);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.IsCompletedByBackupCode).IsTrue();
        await Assert.That(session.IsCompleted).IsTrue();
        await Assert.That(session.ValidUntil).IsEqualTo(Now.AddMinutes(30));
    }

    [Test]
    public async Task CompleteWithBackupCode_Should_ReturnError_When_SessionAlreadyCompleted()
    {
        // Arrange
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);
        session.CompleteWithFactor(MfaMethodType.Email, Now);

        // Act
        var result = session.CompleteWithBackupCode(Now);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.AlreadyCompleted);
    }

    [Test]
    public async Task CompleteWithBackupCode_Should_ReturnError_When_SessionExpired()
    {
        // Arrange
        var session = MfaSession.Create(Guid.NewGuid(), MfaSessionAction.Login, [MfaMethodType.Email], Now);
        var afterExpiry = Now.AddMinutes(6);

        // Act
        var result = session.CompleteWithBackupCode(afterExpiry);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors!.First().Code).IsEqualTo(ErrorCodes.Auth.MfaSession.Expired);
    }
}
