namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Application.Common.Helpers;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

using Microsoft.EntityFrameworkCore;

internal sealed class GetBotsByUserIdHandler(IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetBotsByUserIdQuery, GetBotsByUserIdResult>
{
    public async Task<Result<GetBotsByUserIdResult>> HandleAsync(GetBotsByUserIdQuery query, CancellationToken cancellationToken = default)
    {
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var botInstances = await dbContext.BotInstances
            .Where(x => x.OwnerAccountId == ownerAccountId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = botInstances
            .ConvertAll(b => new GetBotsByUserIdResult.BotListItem(
                b.Id,
                b.ProjectId,
                b.Name,
                b.PlatformType,
                TokenMasker.Mask(b.Credentials.AccessToken),
                b.Credentials is VkBotCredentials vk ? vk.CommunityId : null,
                b.ScenarioVersion,
                b.Status,
                b.ScenarioVersionUpdateMode));

        return new GetBotsByUserIdResult(items);
    }
}
