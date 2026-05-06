namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetBotCredentialsHandler(
    ILogger<GetBotCredentialsHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetBotCredentialsQuery, GetBotCredentialsResult>
{
    public async Task<Result<GetBotCredentialsResult>> HandleAsync(
        GetBotCredentialsQuery query,
        CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(query.BotInstanceId);

        var botInstance = await dbContext.BotInstances
            .SingleOrDefaultAsync(x => x.Id == botInstanceId, cancellationToken);

        if (botInstance is null)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        return new GetBotCredentialsResult(botInstance.Credentials);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);
    #endregion
}
