namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Application.Common.Helpers;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.Common;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetBotHandler(
    ILogger<GetBotHandler> logger,
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetBotQuery, GetBotResult>
{
    public async Task<Result<GetBotResult>> HandleAsync(GetBotQuery query, CancellationToken cancellationToken = default)
    {
        var botInstanceId = Guid.Parse(query.BotInstanceId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var botInstance = await dbContext.BotInstances
            .SingleOrDefaultAsync(x => x.Id == botInstanceId, cancellationToken);

        if (botInstance is null || botInstance.OwnerAccountId != ownerAccountId)
        {
            BotInstanceNotFound(botInstanceId);
            return Error.NotFound(ErrorCodes.BotInstance.NotFound, $"Бот '{botInstanceId}' не найден.");
        }

        var communityId = botInstance.Credentials is VkBotCredentials vk ? vk.CommunityId : null;

        return new GetBotResult(
            botInstance.Id,
            botInstance.ProjectId,
            botInstance.Name,
            botInstance.PlatformType,
            TokenMasker.Mask(botInstance.Credentials.AccessToken),
            communityId,
            botInstance.ScenarioVersion,
            botInstance.Status,
            botInstance.ScenarioVersionUpdateMode);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Бот '{BotInstanceId}' не найден или не принадлежит запросившему пользователю.")]
    private partial void BotInstanceNotFound(Guid botInstanceId);
    #endregion
}
