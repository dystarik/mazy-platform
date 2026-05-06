namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record ResendConfirmationCodeCommand(string Email) : ICommand<ResendConfirmationCodeResult>;
