namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record GetMfaMethodsResult(IReadOnlyCollection<MfaMethodType> Methods);
