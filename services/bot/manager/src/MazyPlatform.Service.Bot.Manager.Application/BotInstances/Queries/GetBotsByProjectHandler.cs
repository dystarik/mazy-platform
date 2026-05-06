namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Application.Common.Helpers;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

using Microsoft.EntityFrameworkCore;

internal sealed class GetBotsByProjectHandler(
    IReadOnlyApplicationDbContext dbContext) : IQueryHandler<GetBotsByProjectQuery, GetBotsByProjectResult>
{
    public async Task<Result<GetBotsByProjectResult>> HandleAsync(GetBotsByProjectQuery query, CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var botInstances = await dbContext.BotInstances
            .Where(x => x.ProjectId == projectId && x.OwnerAccountId == ownerAccountId)
            .ToListAsync(cancellationToken);

        var items = botInstances
            .ConvertAll(b => new GetBotsByProjectResult.BotListItem(
                b.Id,
                b.Name,
                b.PlatformType,
                TokenMasker.Mask(b.Credentials.AccessToken),
                b.Credentials is VkBotCredentials vk ? vk.CommunityId : null,
                b.ScenarioVersion,
                b.Status));

        return new GetBotsByProjectResult(items);
    }
}
