namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record ConfirmEmailCommand(string OtpId, string OtpCode) : ICommand<IReadOnlyCollection<BackupCode>>;
