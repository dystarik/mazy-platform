namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.StreamingQueries;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

public sealed record GetActiveBotsQuery(PlatformType PlatformType) : IStreamingQuery<ActiveBotItem>;
