namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record LogoutCommand(string RefreshTokenId) : ICommand;
