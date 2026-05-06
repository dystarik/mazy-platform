namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

public sealed record GetMfaMethodsQuery(string UserAccountId) : IQuery<GetMfaMethodsResult>;
