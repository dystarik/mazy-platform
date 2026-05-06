namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record RefreshSessionCommand(string RefreshTokenId, string RefreshToken) : ICommand<RefreshSessionResult>;
