namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record StartResult(Guid MfaSessionId, int RequiredFactorCount, IReadOnlyCollection<MfaMethodType> AvailableFactors);
