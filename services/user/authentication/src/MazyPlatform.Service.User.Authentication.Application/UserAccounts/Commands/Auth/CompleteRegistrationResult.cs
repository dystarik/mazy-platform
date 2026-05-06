namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

public sealed record CompleteRegistrationResult(string AccessToken, string RefreshToken);
