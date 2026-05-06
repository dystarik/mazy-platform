namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.LinkedProviders;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record UnlinkProviderCommand(string UserAccountId, ExternalProviderType ProviderType) : ICommand;
