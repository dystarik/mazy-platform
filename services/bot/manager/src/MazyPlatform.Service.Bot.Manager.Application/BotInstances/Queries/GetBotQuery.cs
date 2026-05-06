namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

public sealed record GetBotQuery(string BotInstanceId, string OwnerAccountId) : IQuery<GetBotResult>;
