namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Queries;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

internal sealed partial class GetSessionsHandler(
    IReadOnlyApplicationDbContext readDbContext,
    ILogger<GetSessionsHandler> logger) : IQueryHandler<GetSessionsQuery, GetSessionsResult>
{
    public async Task<Result<GetSessionsResult>> HandleAsync(GetSessionsQuery query, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(query.UserAccountId);
        var currentRefreshTokenId = Guid.Parse(query.RefreshTokenId);

        var sessionsRaw = await readDbContext.UserSessions
            .Where(s => s.UserAccountId == userAccountId && s.RevokedReason == null)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.RefreshTokenId,
                s.CreatedAt,
                IsCurrent = s.RefreshTokenId == currentRefreshTokenId,
            })
            .ToArrayAsync(cancellationToken);

        var sessions = sessionsRaw
            .Select(s => new GetSessionsResult.SessionInfoDto(
                s.RefreshTokenId,
                s.CreatedAt.UtcDateTime,
                s.IsCurrent))
            .ToArray();

        if (sessions.Length is not 0)
            return new GetSessionsResult(sessions);

        UserSessionsNotFound(userAccountId);
        return new GetSessionsResult(sessions);
    }

    [LoggerMessage(1, LogLevel.Warning, "Не найдены сессии для пользователя {UserAccountId}.")]
    private partial void UserSessionsNotFound(Guid userAccountId);
}
