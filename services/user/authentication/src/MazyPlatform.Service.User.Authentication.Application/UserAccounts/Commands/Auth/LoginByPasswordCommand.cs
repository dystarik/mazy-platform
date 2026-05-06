namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record LoginByPasswordCommand(string Email, string Password, string? MfaSessionId) : ICommand<LoginByPasswordResult>;
