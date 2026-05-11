namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed class RegistrationTests : IntegrationTestBase
{
    [Test]
    public async Task RegisterByPassword_Should_ReturnOtpId_AndPublishRegisteredEvent()
    {
        var email = TestUserFactory.UniqueEmail();
        var before = App.Events.Count;

        var response = await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = email,
            Password = TestUserFactory.DefaultPassword,
        });

        var registered = await App.Events.WaitForAsync(RabbitMqEventCapture.Registered, email, before);

        await Assert.That(response.OtpId).IsNotEmpty();
        await Assert.That(Guid.TryParse(response.OtpId, out _)).IsTrue();
        await Assert.That(registered.Email).IsEqualTo(email);
        await Assert.That(registered.UserAccountId).IsNotNull();
        await Assert.That(registered.ConfirmationCode).IsNotEmpty();
    }

    [Test]
    public async Task CompleteRegistration_WithValidOtp_Should_ReturnTokens_AndPublishEmailConfirmed()
    {
        var pending = await App.Users.CreatePendingUserAsync();
        var before = App.Events.Count;

        var response = await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = pending.OtpId,
            OtpCode = pending.Event.ConfirmationCode,
        });

        var confirmed = await App.Events.WaitForAsync(RabbitMqEventCapture.EmailConfirmed, pending.Email, before);

        await Assert.That(response.Tokens.AccessToken).IsNotEmpty();
        await Assert.That(response.Tokens.RefreshToken).IsNotEmpty();
        await Assert.That(confirmed.Email).IsEqualTo(pending.Email);
        await Assert.That(confirmed.UserAccountId).IsEqualTo(pending.Event.UserAccountId);
    }

    [Test]
    public async Task CompleteRegistration_WithWrongOtp_Should_Fail()
    {
        var pending = await App.Users.CreatePendingUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = pending.OtpId,
            OtpCode = "000000",
        }));
    }

    [Test]
    public async Task CompleteRegistration_WithUnknownOtpId_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = Guid.NewGuid().ToString(),
            OtpCode = "000000",
        }));
    }

    [Test]
    public async Task CompleteRegistration_WithAlreadyUsedOtp_Should_Fail()
    {
        var pending = await App.Users.CreatePendingUserAsync();

        await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = pending.OtpId,
            OtpCode = pending.Event.ConfirmationCode,
        });

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = pending.OtpId,
            OtpCode = pending.Event.ConfirmationCode,
        }));
    }

    [Test]
    public async Task CompleteRegistration_WithOldOtpAfterResend_Should_Fail()
    {
        var pending = await App.Users.CreatePendingUserAsync();
        var before = App.Events.Count;

        var resend = await App.Registration.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
        {
            Email = pending.Email,
        });
        var resent = await App.Events.WaitForAsync(RabbitMqEventCapture.Registered, pending.Email, before);

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = pending.OtpId,
            OtpCode = pending.Event.ConfirmationCode,
        }));

        var complete = await App.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = resend.OtpId,
            OtpCode = resent.ConfirmationCode,
        });

        await Assert.That(complete.Tokens.AccessToken).IsNotEmpty();
    }

    [Test]
    public async Task RegisterByPassword_WithExistingConfirmedEmail_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = user.Email,
            Password = TestUserFactory.DefaultPassword,
        }));
    }

    [Test]
    public async Task RegisterByPassword_WithEmailUsedAsConfirmedMfaEmail_Should_Fail()
    {
        var owner = await App.Users.CreateConfirmedUserAsync();
        var mfaEmail = TestUserFactory.UniqueEmail();
        var before = App.Events.Count;

        var addMfa = await App.Mfa.AddFactorAsync(
            new AddFactorRequest
            {
                Factor = MfaFactorType.Email,
                Email = mfaEmail,
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);
        var mfaCode = await App.Events.WaitForAsync(RabbitMqEventCapture.MfaEmailCodeGenerated, mfaEmail, before);

        var confirmMfa = await App.Mfa.ConfirmFactorAsync(
            new ConfirmFactorRequest
            {
                Factor = MfaFactorType.Email,
                Email = new ConfirmEmail
                {
                    OtpId = addMfa.Email.OtpId,
                    Code = mfaCode.Code,
                },
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(confirmMfa.BackupCodes).IsNotEmpty();

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = mfaEmail,
            Password = TestUserFactory.DefaultPassword,
        }));
    }

    [Test]
    public async Task RegisterByPassword_WithRecentPendingEmail_Should_Fail()
    {
        var pending = await App.Users.CreatePendingUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = pending.Email,
            Password = TestUserFactory.DefaultPassword,
        }));
    }

    [Test]
    public async Task ResendConfirmationCode_Should_ReturnNewOtpId_AndPublishRegisteredEvent()
    {
        var pending = await App.Users.CreatePendingUserAsync();
        var before = App.Events.Count;

        var response = await App.Registration.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
        {
            Email = pending.Email,
        });

        var registered = await App.Events.WaitForAsync(RabbitMqEventCapture.Registered, pending.Email, before);

        await Assert.That(response.OtpId).IsNotEmpty();
        await Assert.That(response.OtpId).IsNotEqualTo(pending.OtpId);
        await Assert.That(registered.ConfirmationCode).IsNotEmpty();
    }

    [Test]
    public async Task ResendConfirmationCode_ForConfirmedEmail_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Registration.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
        {
            Email = user.Email,
        }));
    }

    [Test]
    public async Task ResendConfirmationCode_WithInvalidEmail_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Registration.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
        {
            Email = "not-an-email",
        }));
    }

    [Test]
    public async Task RegisterByPassword_WithInvalidEmail_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = "not-an-email",
            Password = TestUserFactory.DefaultPassword,
        }));
    }

    [Test]
    public async Task RegisterByPassword_WithWeakPassword_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = TestUserFactory.UniqueEmail(),
            Password = "weak",
        }));
    }

    [Test]
    public async Task RegisterByPassword_WithEmptyFields_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = string.Empty,
            Password = string.Empty,
        }));
    }
}
