namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using MazyPlatform.Contracts.User.Grpc.Authentication;

public sealed class TestUserFactory
{
    public const string DefaultPassword = "StrongPassword123!";

    private readonly AuthServiceFixture _fixture;

    public TestUserFactory(AuthServiceFixture fixture)
    {
        _fixture = fixture;
    }

    public static string UniqueEmail() => $"test-{Guid.NewGuid():N}@mazy.test";

    public async Task<(string Email, string Password, string OtpId, CapturedAuthEvent Event)> CreatePendingUserAsync(string? email = null)
    {
        var userEmail = email ?? UniqueEmail();
        var before = _fixture.Events.Count;

        var response = await _fixture.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = userEmail,
            Password = DefaultPassword,
        });

        var registered = await _fixture.Events.WaitForAsync(RabbitMqEventCapture.Registered, userEmail, before);
        return (userEmail, DefaultPassword, response.OtpId, registered);
    }

    public async Task<TestUser> CreateConfirmedUserAsync(string? email = null, string? password = null)
    {
        var userEmail = email ?? UniqueEmail();
        var userPassword = password ?? DefaultPassword;
        var before = _fixture.Events.Count;

        var register = await _fixture.Registration.RegisterByPasswordAsync(new RegisterByPasswordRequest
        {
            Email = userEmail,
            Password = userPassword,
        });

        var registered = await _fixture.Events.WaitForAsync(RabbitMqEventCapture.Registered, userEmail, before);
        var beforeComplete = _fixture.Events.Count;

        var complete = await _fixture.Registration.CompleteRegistrationAsync(new CompleteRegistrationRequest
        {
            OtpId = register.OtpId,
            OtpCode = registered.ConfirmationCode,
        });

        await _fixture.Events.WaitForAsync(RabbitMqEventCapture.EmailConfirmed, userEmail, beforeComplete);
        return TestUser.FromTokens(userEmail, userPassword, complete.Tokens);
    }

    public async Task<TestUser> LoginAsync(TestUser user, string? password = null)
    {
        var response = await _fixture.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = password ?? user.Password,
        });

        return TestUser.FromTokens(user.Email, password ?? user.Password, response.Tokens);
    }

    public async Task<TestUser> CreateUserWithEmailMfaAsync()
    {
        var user = await CreateConfirmedUserAsync();

        var response = await _fixture.Mfa.AddFactorAsync(
            new AddFactorRequest { Factor = MfaFactorType.Email },
            GrpcTestMetadata.ForUser(user));

        await Assert.That(response.Email.Success).IsNotNull();
        await Assert.That(response.Email.Success.BackupCodes).IsNotEmpty();

        return user;
    }

    public async Task<(TestUser First, TestUser Second)> CreateUserWithTwoSessionsAsync()
    {
        var first = await CreateConfirmedUserAsync();
        var second = await LoginAsync(first);
        return (first, second);
    }
}
