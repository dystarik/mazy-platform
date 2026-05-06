namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Queries;

public sealed record GetSessionsResult(IReadOnlyCollection<GetSessionsResult.SessionInfoDto> Sessions)
{
    public sealed record SessionInfoDto(Guid RefreshTokenId, DateTime CreatedAt, bool IsCurrent);
}
