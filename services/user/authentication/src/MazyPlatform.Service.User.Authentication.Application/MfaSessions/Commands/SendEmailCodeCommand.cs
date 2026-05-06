namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record SendEmailCodeCommand(string MfaSessionId, string UserAccountId) : ICommand;
