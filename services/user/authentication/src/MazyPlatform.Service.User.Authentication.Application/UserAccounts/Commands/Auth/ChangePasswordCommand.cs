namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record ChangePasswordCommand(string UserAccountId, string CurrentPassword, string NewPassword, string? MfaSessionId) : ICommand<ChangePasswordResult>;
