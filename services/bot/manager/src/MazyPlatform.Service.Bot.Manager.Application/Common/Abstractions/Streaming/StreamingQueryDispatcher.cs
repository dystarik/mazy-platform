namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;

using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;

internal sealed class StreamingQueryDispatcher(IServiceProvider serviceProvider) : IStreamingQueryDispatcher
{
    public async IAsyncEnumerable<TItem> DispatchAsync<TQuery, TItem>(
        TQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TQuery : IStreamingQuery<TItem>
    {
        var handler = serviceProvider.GetRequiredService<IStreamingQueryHandler<TQuery, TItem>>();

        await foreach (var item in handler.HandleAsync(query, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
