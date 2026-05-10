namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using MazyPlatform.Contracts.User.Grpc.Authentication;
using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using OtpNet;

public sealed class TotpMfaTests : IntegrationTestBase
{
    [Test]
    public async Task AddFactorTotp_Should_ReturnProvisioningUri()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var response = await App.Mfa.AddFactorAsync(
            new AddFactorRequest { Factor = MfaFactorType.Totp },
            GrpcTestMetadata.ForUser(user));

        await Assert.That(response.SetupCase).IsEqualTo(AddFactorResponse.SetupOneofCase.Totp);
        await Assert.That(response.Totp.ProvisioningUri).StartsWith("otpauth://");
        await Assert.That(ExtractSecret(response.Totp.ProvisioningUri)).IsNotEmpty();
    }

    [Test]
    public async Task ConfirmFactorTotp_WithWrongCode_Should_Fail()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        _ = await App.Mfa.AddFactorAsync(
            new AddFactorRequest { Factor = MfaFactorType.Totp },
            GrpcTestMetadata.ForUser(user));

        await GrpcAssert.ThrowsAsync(async () => await App.Mfa.ConfirmFactorAsync(
            new ConfirmFactorRequest
            {
                Factor = MfaFactorType.Totp,
                Totp = new ConfirmTotp { Code = "000000" },
            },
            GrpcTestMetadata.ForUser(user)));
    }

    [Test]
    public async Task ConfirmFactorTotp_WithValidCode_Should_EnableTotpMfa()
    {
        var user = await App.Users.CreateConfirmedUserAsync();

        var add = await App.Mfa.AddFactorAsync(
            new AddFactorRequest { Factor = MfaFactorType.Totp },
            GrpcTestMetadata.ForUser(user));
        var code = CreateTotpCode(add.Totp.ProvisioningUri);

        var confirm = await App.Mfa.ConfirmFactorAsync(
            new ConfirmFactorRequest
            {
                Factor = MfaFactorType.Totp,
                Totp = new ConfirmTotp { Code = code },
            },
            GrpcTestMetadata.ForUser(user));
        var login = await App.Authentication.LoginByPasswordAsync(new LoginByPasswordRequest
        {
            Email = user.Email,
            Password = user.Password,
        });

        await Assert.That(confirm.BackupCodes).IsNotEmpty();
        await Assert.That(login.ResultCase).IsEqualTo(LoginByPasswordResponse.ResultOneofCase.Challenge);
        await Assert.That(login.Challenge.AvailableFactors.Contains(MfaFactorType.Totp)).IsTrue();
    }

    private static string CreateTotpCode(string provisioningUri)
    {
        var secret = ExtractSecret(provisioningUri);
        return new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(DateTime.UtcNow);
    }

    private static string ExtractSecret(string provisioningUri)
    {
        var queryStart = provisioningUri.IndexOf('?', StringComparison.Ordinal);
        if (queryStart < 0)
            return string.Empty;

        var query = provisioningUri[(queryStart + 1)..];
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(pair[0], "secret", StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(pair[1]);
        }

        return string.Empty;
    }
}
