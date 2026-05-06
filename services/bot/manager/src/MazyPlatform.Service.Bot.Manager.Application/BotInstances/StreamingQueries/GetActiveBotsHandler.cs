namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.StreamingQueries;

using System.Runtime.CompilerServices;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetActiveBotsHandler(
    ILogger<GetActiveBotsHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IStreamingQueryHandler<GetActiveBotsQuery, ActiveBotItem>
{
    public async IAsyncEnumerable<ActiveBotItem> HandleAsync(
        GetActiveBotsQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        StreamingStarted(query.PlatformType);

        var bots = dbContext.BotInstances
            .Where(b => b.Status == BotStatus.Active
                     && b.ProjectId != null
                     && b.ScenarioVersion != null)
            .AsAsyncEnumerable();

        var streamedCount = 0;

        await foreach (var bot in bots.WithCancellation(cancellationToken))
        {
            // PlatformType вычисляется из JSONB-поля Credentials и не транслируется в SQL.
            if (bot.PlatformType != query.PlatformType)
                continue;

            yield return new ActiveBotItem(
                bot.Id,
                bot.ProjectId!.Value,
                bot.ScenarioVersion!.Value,
                bot.Credentials);

            streamedCount++;
        }

        StreamingCompleted(query.PlatformType, streamedCount);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Начат стриминг активных ботов для платформы {PlatformType}.")]
    private partial void StreamingStarted(PlatformType platformType);

    [LoggerMessage(2, LogLevel.Information, "Завершён стриминг активных ботов для платформы {PlatformType}, всего отдано {Count}.")]
    private partial void StreamingCompleted(PlatformType platformType, int count);
    #endregion
}
