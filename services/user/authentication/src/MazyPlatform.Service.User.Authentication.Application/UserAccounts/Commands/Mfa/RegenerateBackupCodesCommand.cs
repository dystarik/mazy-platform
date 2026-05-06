namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record RegenerateBackupCodesCommand(string UserAccountId, string MfaSessionId) : ICommand<IReadOnlyCollection<BackupCode>>;
