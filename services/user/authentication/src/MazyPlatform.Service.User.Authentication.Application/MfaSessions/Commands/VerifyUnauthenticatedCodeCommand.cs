namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record VerifyUnauthenticatedCodeCommand(string MfaSessionId, MfaMethodType MfaMethodType, string Code) : ICommand<VerifyCodeResult>;
