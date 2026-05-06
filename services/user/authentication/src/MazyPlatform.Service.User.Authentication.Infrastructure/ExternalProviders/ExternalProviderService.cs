namespace MazyPlatform.Service.User.Authentication.Infrastructure.ExternalProviders;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

internal sealed class ExternalProviderService(IEnumerable<IProviderHandler> providerHandlers) : IExternalProviderService
{
    private readonly Dictionary<ExternalProviderType, IProviderHandler> _providerHandlers = providerHandlers.ToDictionary(h => h.ProviderType);

    public async Task<Email?> GetEmailAsync(ExternalProviderType providerType, string code, CancellationToken cancellationToken)
    {
        return await _providerHandlers[providerType].GetEmailAsync(code, cancellationToken);
    }
}
