namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;

public sealed record CreateBotCommand(
    string OwnerAccountId,
    string ProjectId,
    string Name,
    PlatformType PlatformType,
    string AccessToken,
    string? CommunityId,
    int ScenarioVersion) : ICommand<CreateBotResult>;
