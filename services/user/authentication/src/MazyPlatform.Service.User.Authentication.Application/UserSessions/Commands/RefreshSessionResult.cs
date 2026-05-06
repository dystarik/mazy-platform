namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

public sealed record RefreshSessionResult(string AccessToken, string RefreshToken);
