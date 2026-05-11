namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using Google.Protobuf.WellKnownTypes;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed class EmailMfaTests : IntegrationTestBase
{
    [Test]
    public async Task AddEmailFactor_WithAccountEmail_Should_EnableEmailMfa()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var add = await App.Mfa.AddFactorAsync(
            new AddFactorRequest { Factor = MfaFactorType.Email },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);
        var factors = await App.Mfa.GetFactorsAsync(
            new Empty(),
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(add.SetupCase).IsEqualTo(AddFactorResponse.SetupOneofCase.Email);
        await Assert.That(add.Email.ResultCase).IsEqualTo(EmailMfaSetup.ResultOneofCase.Success);
        await Assert.That(add.Email.Success.BackupCodes).IsNotEmpty();
        await Assert.That(factors.Factors.Contains(MfaFactorType.Email)).IsTrue();
    }

    [Test]
    public async Task AddEmailFactor_WithInvalidCustomEmail_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.AddFactorAsync(
            new AddFactorRequest
            {
                Factor = MfaFactorType.Email,
                Email = "not-an-email",
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task ConfirmEmailFactor_WithWrongCode_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();
        var mfaEmail = TestUserFactory.UniqueEmail();

        var add = await App.Mfa.AddFactorAsync(
            new AddFactorRequest
            {
                Factor = MfaFactorType.Email,
                Email = mfaEmail,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.ConfirmFactorAsync(
            new ConfirmFactorRequest
            {
                Factor = MfaFactorType.Email,
                Email = new ConfirmEmail
                {
                    OtpId = add.Email.OtpId,
                    Code = "000000",
                },
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task LoginByPassword_WithEmailMfa_Should_ReturnChallenge()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();

        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        });

        await Assert.That(login.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Challenge);
        await Assert.That(login.Challenge.MfaSessionId).IsNotEmpty();
        await Assert.That(login.Challenge.RequiredFactorCount).IsGreaterThan(0);
        await Assert.That(login.Challenge.AvailableFactors.Contains(MfaFactorType.Email)).IsTrue();
    }

    [Test]
    public async Task SendUnauthenticatedEmailCode_Should_PublishMfaEmailCodeGenerated()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var login = await LoginForChallengeAsync(user);
        var before = App.Events.Count;

        await App.MfaSession.SendUnauthenticatedEmailCodeAsync(new SendUnauthenticatedEmailCodeRequest
        {
            MfaSessionId = login.Challenge.MfaSessionId,
        });

        var code = await App.Events.WaitForAsync(RabbitMqEventCapture.MfaEmailCodeGenerated, user.Email, before);

        await Assert.That(code.Code).IsNotEmpty();
    }

    [Test]
    public async Task VerifyUnauthenticatedCode_WithValidCode_Should_CompleteMfaSession()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var challenge = await SendUnauthenticatedEmailCodeAsync(user);

        var verify = await App.MfaSession.VerifyUnauthenticatedCodeAsync(
            new VerifyUnauthenticatedCodeRequest
            {
                MfaSessionId = challenge.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = challenge.Code,
            },
            deadline: GrpcTestCall.Deadline);

        await Assert.That(verify.IsCompleted).IsTrue();
    }

    [Test]
    public async Task VerifyUnauthenticatedCode_WithWrongCode_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var challenge = await SendUnauthenticatedEmailCodeAsync(user);

        await GrpcAssert.ThrowsAsync(async () => await App.MfaSession.VerifyUnauthenticatedCodeAsync(
            new VerifyUnauthenticatedCodeRequest
            {
                MfaSessionId = challenge.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = "000000",
            },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task LoginByPassword_WithCompletedMfaSession_Should_ReturnTokens()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var challenge = await SendUnauthenticatedEmailCodeAsync(user);

        _ = await App.MfaSession.VerifyUnauthenticatedCodeAsync(
            new VerifyUnauthenticatedCodeRequest
            {
                MfaSessionId = challenge.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = challenge.Code,
            },
            deadline: GrpcTestCall.Deadline);

        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
            MfaSessionId = challenge.MfaSessionId,
        });

        await Assert.That(login.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Tokens);
        await Assert.That(login.Tokens.AccessToken).IsNotEmpty();
    }

    [Test]
    public async Task LoginByPassword_WithUnknownMfaSessionId_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(
            new LoginByPasswordRequest
            {
                Email = user.Email,
                Password = user.Password,
                MfaSessionId = Guid.NewGuid().ToString(),
            },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task LoginByPassword_WithCompletedMfaSessionOwnedByAnotherUser_Should_Fail()
    {
        var first = await App.Users.CreateUserWithEmailMfaAsync();
        var second = await App.Users.CreateUserWithEmailMfaAsync();
        var firstChallenge = await SendUnauthenticatedEmailCodeAsync(first);

        _ = await App.MfaSession.VerifyUnauthenticatedCodeAsync(
            new VerifyUnauthenticatedCodeRequest
            {
                MfaSessionId = firstChallenge.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = firstChallenge.Code,
            },
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(
            new LoginByPasswordRequest
            {
                Email = second.Email,
                Password = second.Password,
                MfaSessionId = firstChallenge.MfaSessionId,
            },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task LoginByPassword_WithAuthenticatedActionMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var completed = await CompleteAuthenticatedEmailMfaAsync(user, MfaSessionAction.ChangePassword);

        await GrpcAssert.ThrowsAsync(async () => await App.Authentication.LoginByPasswordAsync(
            new LoginByPasswordRequest
            {
                Email = user.Email,
                Password = user.Password,
                MfaSessionId = completed.MfaSessionId,
            },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task GetStatus_AfterVerify_Should_ReturnCompletedFactor()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var challenge = await SendUnauthenticatedEmailCodeAsync(user);

        _ = await App.MfaSession.VerifyUnauthenticatedCodeAsync(
            new VerifyUnauthenticatedCodeRequest
            {
                MfaSessionId = challenge.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = challenge.Code,
            },
            deadline: GrpcTestCall.Deadline);

        var status = await App.MfaSession.GetStatusAsync(
            new GetStatusRequest { MfaSessionId = challenge.MfaSessionId },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(status.IsCompleted).IsTrue();
        await Assert.That(status.CompletedFactors.Contains(MfaFactorType.Email)).IsTrue();
    }

    [Test]
    public async Task Start_Should_CreateAuthenticatedSessions_ForProtectedActions()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();

        foreach (var action in new[]
        {
            MfaSessionAction.ChangePassword,
            MfaSessionAction.RemoveFactor,
            MfaSessionAction.RegenerateBackupCodes,
        })
        {
            var response = await App.MfaSession.StartAsync(
                new StartRequest { Action = action },
                GrpcTestMetadata.ForUser(user),
                deadline: GrpcTestCall.Deadline);

            await Assert.That(response.MfaSessionId).IsNotEmpty();
            await Assert.That(response.RequiredFactorCount).IsGreaterThan(0);
            await Assert.That(response.AvailableFactors.Contains(MfaFactorType.Email)).IsTrue();
        }
    }

    [Test]
    public async Task SendEmailCode_AndVerifyCode_ForAuthenticatedSession_Should_CompleteSession()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var completed = await CompleteAuthenticatedEmailMfaAsync(user, MfaSessionAction.ChangePassword);

        await Assert.That(completed.IsCompleted).IsTrue();
        await Assert.That(completed.Code).IsNotEmpty();
    }

    [Test]
    public async Task SendEmailCode_WithSessionOwnedByAnotherUser_Should_Fail()
    {
        var first = await App.Users.CreateUserWithEmailMfaAsync();
        var second = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RemoveFactor },
            GrpcTestMetadata.ForUser(first),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.MfaSession.SendEmailCodeAsync(
            new SendEmailCodeRequest { MfaSessionId = start.MfaSessionId },
            GrpcTestMetadata.ForUser(second),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task VerifyCode_WithSessionOwnedByAnotherUser_Should_Fail()
    {
        var first = await App.Users.CreateUserWithEmailMfaAsync();
        var second = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RemoveFactor },
            GrpcTestMetadata.ForUser(first),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.MfaSession.VerifyCodeAsync(
            new VerifyCodeRequest
            {
                MfaSessionId = start.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = "000000",
            },
            GrpcTestMetadata.ForUser(second),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task SendUnauthenticatedEmailCode_WithAuthenticatedSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RemoveFactor },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.MfaSession.SendUnauthenticatedEmailCodeAsync(
            new SendUnauthenticatedEmailCodeRequest { MfaSessionId = start.MfaSessionId },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task GetStatus_WithSessionOwnedByAnotherUser_Should_Fail()
    {
        var first = await App.Users.CreateUserWithEmailMfaAsync();
        var second = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RemoveFactor },
            GrpcTestMetadata.ForUser(first),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.MfaSession.GetStatusAsync(
            new GetStatusRequest { MfaSessionId = start.MfaSessionId },
            GrpcTestMetadata.ForUser(second),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task RemoveFactor_AfterCompletedMfaSession_Should_RemoveEmailMfa()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var completed = await CompleteAuthenticatedEmailMfaAsync(user, MfaSessionAction.RemoveFactor);

        await App.Mfa.RemoveFactorAsync(
            new RemoveFactorRequest
            {
                MfaSessionId = completed.MfaSessionId,
                Factor = MfaFactorType.Email,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        var factors = await App.Mfa.GetFactorsAsync(
            new Empty(),
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);
        await Assert.That(factors.Factors.Contains(MfaFactorType.Email)).IsFalse();
    }

    [Test]
    public async Task RemoveFactor_WithoutCompletedMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RemoveFactor },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.RemoveFactorAsync(
            new RemoveFactorRequest
            {
                MfaSessionId = start.MfaSessionId,
                Factor = MfaFactorType.Email,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task RemoveFactor_WithWrongActionMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var completed = await CompleteAuthenticatedEmailMfaAsync(user, MfaSessionAction.ChangePassword);

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.RemoveFactorAsync(
            new RemoveFactorRequest
            {
                MfaSessionId = completed.MfaSessionId,
                Factor = MfaFactorType.Email,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task RegenerateBackupCodes_AfterCompletedMfaSession_Should_ReturnNewCodes()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var completed = await CompleteAuthenticatedEmailMfaAsync(user, MfaSessionAction.RegenerateBackupCodes);

        var response = await App.Mfa.RegenerateBackupCodesAsync(
            new RegenerateBackupCodesRequest { MfaSessionId = completed.MfaSessionId },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.BackupCodes).IsNotEmpty();
    }

    [Test]
    public async Task RegenerateBackupCodes_WithoutCompletedMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = MfaSessionAction.RegenerateBackupCodes },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.RegenerateBackupCodesAsync(
            new RegenerateBackupCodesRequest { MfaSessionId = start.MfaSessionId },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task RegenerateBackupCodes_WithWrongActionMfaSession_Should_Fail()
    {
        var user = await App.Users.CreateUserWithEmailMfaAsync();
        var completed = await CompleteAuthenticatedEmailMfaAsync(user, MfaSessionAction.RemoveFactor);

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.RegenerateBackupCodesAsync(
            new RegenerateBackupCodesRequest { MfaSessionId = completed.MfaSessionId },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline));
    }

    private static async Task<LoginByPasswordResponse> LoginForChallengeAsync(TestUser user)
    {
        var response = await App.Authentication.LoginByPasswordAsync(
            new LoginByPasswordRequest
            {
                Email = user.Email,
                Password = user.Password,
            },
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Challenge);
        return response;
    }

    private static async Task<(string MfaSessionId, string Code)> SendUnauthenticatedEmailCodeAsync(TestUser user)
    {
        var login = await LoginForChallengeAsync(user);
        var before = App.Events.Count;

        await App.MfaSession.SendUnauthenticatedEmailCodeAsync(
            new SendUnauthenticatedEmailCodeRequest
            {
                MfaSessionId = login.Challenge.MfaSessionId,
            },
            deadline: GrpcTestCall.Deadline);

        var code = await App.Events.WaitForAsync(RabbitMqEventCapture.MfaEmailCodeGenerated, user.Email, before);
        return (login.Challenge.MfaSessionId, code.Code!);
    }

    private static async Task<(string MfaSessionId, string Code, bool IsCompleted)> CompleteAuthenticatedEmailMfaAsync(
        TestUser user,
        MfaSessionAction action)
    {
        var start = await App.MfaSession.StartAsync(
            new StartRequest { Action = action },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);
        var before = App.Events.Count;

        await App.MfaSession.SendEmailCodeAsync(
            new SendEmailCodeRequest { MfaSessionId = start.MfaSessionId },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        var code = await App.Events.WaitForAsync(RabbitMqEventCapture.MfaEmailCodeGenerated, user.Email, before);
        var verify = await App.MfaSession.VerifyCodeAsync(
            new VerifyCodeRequest
            {
                MfaSessionId = start.MfaSessionId,
                Factor = MfaFactorType.Email,
                Code = code.Code,
            },
            GrpcTestMetadata.ForUser(user),
            deadline: GrpcTestCall.Deadline);

        return (start.MfaSessionId, code.Code!, verify.IsCompleted);
    }
}
