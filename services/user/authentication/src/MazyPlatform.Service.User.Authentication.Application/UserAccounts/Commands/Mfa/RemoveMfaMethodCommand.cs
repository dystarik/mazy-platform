namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record RemoveMfaMethodCommand(string MfaSessionId, string UserAccountId, MfaMethodType MfaMethodType) : ICommand;
