namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record LogoutAllCommand(string UserAccountId, string CurrentRefreshTokenId, bool ExcludeCurrentSession) : ICommand;
