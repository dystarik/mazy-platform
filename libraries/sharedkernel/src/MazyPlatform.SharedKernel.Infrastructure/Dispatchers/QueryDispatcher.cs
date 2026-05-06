namespace MazyPlatform.SharedKernel.Infrastructure.Dispatchers;

using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.DependencyInjection;

internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<Result<TResponse>> DispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TResponse : notnull
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.HandleAsync(query, cancellationToken);
    }
}
