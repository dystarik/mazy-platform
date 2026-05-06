namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record RegisterByPasswordCommand(string Email, string Password) : ICommand<RegisterByPasswordResult>;
