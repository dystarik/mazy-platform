namespace MazyPlatform.Service.User.Authentication.Infrastructure.ExternalProviders;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

internal interface IProviderHandler
{
    ExternalProviderType ProviderType { get; }

    Task<Email?> GetEmailAsync(string code, CancellationToken cancellationToken);
}
