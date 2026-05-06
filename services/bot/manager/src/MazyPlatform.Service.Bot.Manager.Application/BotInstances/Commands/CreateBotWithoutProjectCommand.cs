namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record CreateBotWithoutProjectCommand(
    string OwnerAccountId,
    string Name,
    PlatformType PlatformType,
    string AccessToken,
    string? CommunityId,
    ScenarioVersionUpdateMode ScenarioVersionUpdateMode) : ICommand<CreateBotResult>;
