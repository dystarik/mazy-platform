namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record ChangePasswordResult
{
    public ChangePasswordResult(Guid mfaSessionId, int requiredFactorCount, IReadOnlyCollection<MfaMethodType> availableFactors)
    {
        RequiresMfa = true;
        MfaRequiredData = new MfaRequired(mfaSessionId, requiredFactorCount, availableFactors);
    }

    public ChangePasswordResult()
    {
        RequiresMfa = false;
    }

    [MemberNotNullWhen(true, nameof(MfaRequiredData))]
    public bool RequiresMfa { get; init; }

    public MfaRequired? MfaRequiredData { get; init; }

    public sealed record MfaRequired(Guid MfaSessionId, int RequiredFactorCount, IReadOnlyCollection<MfaMethodType> AvailableFactors);
}
