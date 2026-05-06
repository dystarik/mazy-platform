namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record LoginByExternalProviderCommand(ExternalProviderType ProviderType, string Code) : ICommand<LoginByExternalProviderResult>;
