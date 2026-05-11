namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed class PasswordTests : IntegrationTestBase
{
    [Test]
    public async Task ChangePassword_WithValidCurrentPassword_Should_ChangePassword()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        const string newPassword = "NewStrongPassword123!";

        var response = await App.Password.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                CurrentPassword = user.Password,
                NewPassword = newPassword,
            },
            GrpcTestMetadata.ForUser(user));

        await Assert.That(response.ResultCase).IsEqualTo(ChangePasswordResponse.ResultOneofCase.Success);
        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        }));

        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = newPassword,
        });
        await Assert.That(login.Tokens.AccessToken).IsNotEmpty();
    }

    [Test]
    public async Task ChangePassword_WithWrongCurrentPassword_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewStrongPassword123!",
            },
            GrpcTestMetadata.ForUser(user)));
    }

    [Test]
    public async Task ChangePassword_WithWeakNewPassword_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                CurrentPassword = user.Password,
                NewPassword = "weak",
            },
            GrpcTestMetadata.ForUser(user)));
    }

    [Test]
    public async Task ChangePassword_WithMfaEnabled_Should_ReturnChallenge_AndRequireCompletedMfaSession()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        const string newPassword = "MfaChangedPassword123!";

        var challenge = await App.Password.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                CurrentPassword = user.Password,
                NewPassword = newPassword,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(challenge.ResultCase).IsEqualTo(ChangePasswordResponse.ResultOneofCase.Challenge);
        await Assert.That(challenge.Challenge.MfaSessionId).IsNotEmpty();

        await CompleteAuthenticatedEmailMfaAsync(user, challenge.Challenge.MfaSessionId);

        var changed = await App.Password.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                CurrentPassword = user.Password,
                NewPassword = newPassword,
                MfaSessionId = challenge.Challenge.MfaSessionId,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(changed.ResultCase).IsEqualTo(ChangePasswordResponse.ResultOneofCase.Success);
        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        }));

        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = newPassword,
        });
        await Assert.That(login.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Challenge);
    }

    [Test]
    public async Task ChangePassword_WithWrongActionCompletedMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RemoveFactor },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await CompleteAuthenticatedEmailMfaAsync(user, start.MfaSessionId);

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                CurrentPassword = user.Password,
                NewPassword = "WrongActionPassword123!",
                MfaSessionId = start.MfaSessionId,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task ResetPassword_ForExistingUser_Should_ReturnOtpChallenge_AndPublishPasswordResetEvent()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        var before = App.Events.Count;

        var response = await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
        });

        var reset = await App.Events.WaitForAsync(RabbitMqEventCapture.PasswordResetRequested, user.Email, before);

        await Assert.That(response.ResultCase).IsEqualTo(ResetPasswordResponse.ResultOneofCase.Otp);
        await Assert.That(response.Otp.OtpId).IsNotEmpty();
        await Assert.That(reset.ResetCode).IsNotEmpty();
    }

    [Test]
    public async Task ConfirmResetPassword_WithValidOtp_Should_ChangePassword()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        const string newPassword = "ResetStrongPassword123!";
        var before = App.Events.Count;

        var resetResponse = await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
        });
        var reset = await App.Events.WaitForAsync(RabbitMqEventCapture.PasswordResetRequested, user.Email, before);

        await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = newPassword,
            Otp = new OtpConfirmation
            {
                OtpId = resetResponse.Otp.OtpId,
                Code = reset.ResetCode,
            },
        });

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        }));

        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = newPassword,
        });
        await Assert.That(login.Tokens.AccessToken).IsNotEmpty();
    }

    [Test]
    public async Task ConfirmResetPassword_WithAlreadyUsedOtp_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        const string newPassword = "ResetStrongPassword123!";
        var before = App.Events.Count;

        var resetResponse = await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
        });
        var reset = await App.Events.WaitForAsync(RabbitMqEventCapture.PasswordResetRequested, user.Email, before);

        await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = newPassword,
            Otp = new OtpConfirmation
            {
                OtpId = resetResponse.Otp.OtpId,
                Code = reset.ResetCode,
            },
        });

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = "AnotherResetPassword123!",
            Otp = new OtpConfirmation
            {
                OtpId = resetResponse.Otp.OtpId,
                Code = reset.ResetCode,
            },
        }));
    }

    [Test]
    public async Task ConfirmResetPassword_WithWrongOtp_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var resetResponse = await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
        });

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = "ResetStrongPassword123!",
            Otp = new OtpConfirmation
            {
                OtpId = resetResponse.Otp.OtpId,
                Code = "000000",
            },
        }));
    }

    [Test]
    public async Task ConfirmResetPassword_WithWeakNewPassword_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        var before = App.Events.Count;

        var resetResponse = await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
        });
        var reset = await App.Events.WaitForAsync(RabbitMqEventCapture.PasswordResetRequested, user.Email, before);

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = "weak",
            Otp = new OtpConfirmation
            {
                OtpId = resetResponse.Otp.OtpId,
                Code = reset.ResetCode,
            },
        }));
    }

    [Test]
    public async Task ResetPassword_ForUserWithMfa_Should_ReturnMfaChallenge_AndConfirmWithCompletedMfa()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        const string newPassword = "MfaResetPassword123!";

        var resetResponse = await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
        });

        await Assert.That(resetResponse.ResultCase).IsEqualTo(ResetPasswordResponse.ResultOneofCase.Mfa);
        await Assert.That(resetResponse.Mfa.MfaSessionId).IsNotEmpty();
        await Assert.That(resetResponse.Mfa.AvailableFactors.Contains(MfaFactorType.Email)).IsTrue();

        await CompleteUnauthenticatedEmailMfaAsync(user.Email, resetResponse.Mfa.MfaSessionId);

        await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = newPassword,
            Mfa = new MfaConfirmation
            {
                MfaSessionId = resetResponse.Mfa.MfaSessionId,
            },
        });

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        }));

        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = newPassword,
        });
        await Assert.That(login.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Challenge);
    }

    [Test]
    public async Task ConfirmResetPassword_WithWrongActionMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        });

        await CompleteUnauthenticatedEmailMfaAsync(user.Email, login.Challenge.MfaSessionId);

        await GrpcAssert.ThrowsAsync(async () => await App.Password.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            NewPassword = "WrongActionResetPassword123!",
            Mfa = new MfaConfirmation
            {
                MfaSessionId = login.Challenge.MfaSessionId,
            },
        }));
    }

    [Test]
    public async Task ResetPassword_ForUnknownEmail_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Password.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = TestUserFactory.UniqueEmail(),
        }));
    }

    private static async Task CompleteAuthenticatedEmailMfaAsync(TestUser user, string mfaSessionId)
    {
        var before = App.Events.Count;

        await App.MfaSession.SendEmailCodeAsync(
            new SendEmailCodeRequest { MfaSessionId = mfaSessionId },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        var code = await App.Events.WaitForAsync(RabbitMqEventCapture.MfaEmailCodeGenerated, user.Email, before);
        var verify = await App.MfaSession.VerifyCodeAsync(
            new VerifyCodeRequest
            {
                MfaSessionId = mfaSessionId,
                Factor = MfaFactorType.Email,
                Code = code.Code,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(verify.IsCompleted).IsTrue();
    }

    private static async Task CompleteUnauthenticatedEmailMfaAsync(string email, string mfaSessionId)
    {
        var before = App.Events.Count;

        await App.MfaSession.SendUnauthenticatedEmailCodeAsync(
            new SendUnauthenticatedEmailCodeRequest { MfaSessionId = mfaSessionId },
            deadline: GrpcTestCall.Deadline);

        var code = await App.Events.WaitForAsync(RabbitMqEventCapture.MfaEmailCodeGenerated, email, before);
        var verify = await App.MfaSession.VerifyUnauthenticatedCodeAsync(
            new VerifyUnauthenticatedCodeRequest
            {
                MfaSessionId = mfaSessionId,
                Factor = MfaFactorType.Email,
                Code = code.Code,
            },
            deadline: GrpcTestCall.Deadline);

        await Assert.That(verify.IsCompleted).IsTrue();
    }
}
