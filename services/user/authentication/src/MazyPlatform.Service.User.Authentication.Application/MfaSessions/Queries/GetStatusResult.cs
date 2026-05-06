namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Queries;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record GetStatusResult(
    bool IsCompleted,
    bool IsExpired,
    IReadOnlyCollection<MfaMethodType> AvailableFactors,
    IReadOnlyCollection<MfaMethodType> CompletedFactors,
    int RequiredFactorCount);
