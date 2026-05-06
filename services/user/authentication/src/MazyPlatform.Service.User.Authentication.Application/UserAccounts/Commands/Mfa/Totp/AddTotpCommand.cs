namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record AddTotpCommand(string UserAccountId) : ICommand<AddTotpResult>;
