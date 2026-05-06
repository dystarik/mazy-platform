namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record ResetPasswordResult
{
    public ResetPasswordResult(Guid otpId)
    {
        RequiresMfa = false;
        OtpChallengeData = new OtpChallenge(otpId);
    }

    public ResetPasswordResult(Guid mfaSessionId, int requiredFactorCount, IReadOnlyCollection<MfaMethodType> availableFactors)
    {
        RequiresMfa = true;
        MfaChallengeData = new MfaChallenge(mfaSessionId, requiredFactorCount, availableFactors);
    }

    [MemberNotNullWhen(true, nameof(MfaChallengeData))]
    [MemberNotNullWhen(false, nameof(OtpChallengeData))]
    public bool RequiresMfa { get; init; }

    public OtpChallenge? OtpChallengeData { get; init; }

    public MfaChallenge? MfaChallengeData { get; init; }

    public sealed record OtpChallenge(Guid OtpId);

    public sealed record MfaChallenge(Guid MfaSessionId, int RequiredFactorCount, IReadOnlyCollection<MfaMethodType> AvailableFactors);
}
