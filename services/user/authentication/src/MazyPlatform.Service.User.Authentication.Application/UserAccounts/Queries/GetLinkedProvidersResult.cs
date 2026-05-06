namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;

public sealed record GetLinkedProvidersResult(IReadOnlyCollection<ExternalProviderType> Providers);
