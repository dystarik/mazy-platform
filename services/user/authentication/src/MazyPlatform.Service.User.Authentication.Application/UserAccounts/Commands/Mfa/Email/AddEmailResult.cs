namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

public sealed record AddEmailResult(Guid? OtpId, IReadOnlyCollection<BackupCode>? BackupCodes, bool IsVerificationRequired);
