namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record SetPasswordResult
{
    public SetPasswordResult(Guid mfaSessionId, int requiredFactorCount, IReadOnlyCollection<MfaMethodType> availableFactors)
    {
        RequiresMfa = true;
        MfaRequiredData = new MfaRequired(mfaSessionId, requiredFactorCount, availableFactors);
    }

    public SetPasswordResult()
    {
        RequiresMfa = false;
    }

    [MemberNotNullWhen(true, nameof(MfaRequiredData))]
    public bool RequiresMfa { get; init; }

    public MfaRequired? MfaRequiredData { get; init; }

    public sealed record MfaRequired(Guid MfaSessionId, int RequiredFactorCount, IReadOnlyCollection<MfaMethodType> AvailableFactors);
}
