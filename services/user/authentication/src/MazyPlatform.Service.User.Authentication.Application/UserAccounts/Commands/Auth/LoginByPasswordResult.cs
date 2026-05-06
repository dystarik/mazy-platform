namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record LoginByPasswordResult
{
    public LoginByPasswordResult(Guid mfaSessionId, int requiredFactorCount, IReadOnlyCollection<MfaMethodType> availableFactor)
    {
        RequiresMfa = true;
        MfaRequiredData = new MfaRequired(mfaSessionId, requiredFactorCount, availableFactor);
    }

    public LoginByPasswordResult(string accessToken, string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        RequiresMfa = false;
        SuccessData = new Success(accessToken, refreshToken);
    }

    [MemberNotNullWhen(true, nameof(MfaRequiredData))]
    [MemberNotNullWhen(false, nameof(SuccessData))]
    public bool RequiresMfa { get; init; }

    public MfaRequired? MfaRequiredData { get; init; }

    public Success? SuccessData { get; init; }

    public sealed record MfaRequired(Guid MfaSessionId, int RequiredFactorCount, IReadOnlyCollection<MfaMethodType> AvailableFactors);

    public sealed record Success(string AccessToken, string RefreshToken);
}
