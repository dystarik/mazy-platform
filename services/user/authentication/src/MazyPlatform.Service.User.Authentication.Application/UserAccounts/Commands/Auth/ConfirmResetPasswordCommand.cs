namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record ConfirmResetPasswordCommand(string NewPassword, string? OtpId, string? OtpCode, string? MfaSessionId) : ICommand;
