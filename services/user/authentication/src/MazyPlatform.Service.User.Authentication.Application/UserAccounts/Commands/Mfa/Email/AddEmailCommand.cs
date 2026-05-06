namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record AddEmailCommand(string UserAccountId, string? Email) : ICommand<AddEmailResult>;
