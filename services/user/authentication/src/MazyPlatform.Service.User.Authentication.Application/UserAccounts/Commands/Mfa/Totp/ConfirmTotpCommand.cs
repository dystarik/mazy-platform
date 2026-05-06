namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record ConfirmTotpCommand(string UserAccountId, string Code) : ICommand<IReadOnlyCollection<BackupCode>>;
