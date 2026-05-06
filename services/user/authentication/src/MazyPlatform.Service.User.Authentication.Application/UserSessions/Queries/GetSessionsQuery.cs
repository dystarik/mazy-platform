namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Queries;

using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

public sealed record GetSessionsQuery(string UserAccountId, string RefreshTokenId) : IQuery<GetSessionsResult>;
