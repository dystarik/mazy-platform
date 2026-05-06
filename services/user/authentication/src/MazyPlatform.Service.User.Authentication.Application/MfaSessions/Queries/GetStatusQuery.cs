namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Queries;

using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

public sealed record GetStatusQuery(string UserAccountId, string MfaSessionId) : IQuery<GetStatusResult>;
