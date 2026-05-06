namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record SetPasswordCommand(string UserAccountId, string NewPassword, string? MfaSessionId) : ICommand<SetPasswordResult>;
