namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record StartCommand(MfaSessionAction Action, string UserAccountId) : ICommand<StartResult>;
