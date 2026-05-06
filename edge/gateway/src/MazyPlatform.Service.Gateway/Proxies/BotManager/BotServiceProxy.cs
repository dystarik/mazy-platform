namespace MazyPlatform.Service.Gateway.Proxies.BotManager;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;

using Microsoft.AspNetCore.Authorization;

[Authorize]
internal sealed class BotServiceProxy(BotService.BotServiceClient client) : BotService.BotServiceBase
{
    public override Task<CreateBotResponse> CreateBot(CreateBotRequest request, ServerCallContext context)
        => client.CreateBotAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<CreateBotResponse> CreateBotWithoutProject(CreateBotWithoutProjectRequest request, ServerCallContext context)
        => client.CreateBotWithoutProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetBotResponse> GetBot(GetBotRequest request, ServerCallContext context)
        => client.GetBotAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetBotsByProjectResponse> GetBotsByProject(GetBotsByProjectRequest request, ServerCallContext context)
        => client.GetBotsByProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<GetBotsByUserIdResponse> GetBotsByUserId(Empty request, ServerCallContext context)
        => client.GetBotsByUserIdAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> BindBotToProject(BindBotToProjectRequest request, ServerCallContext context)
        => client.BindBotToProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> UnbindBotFromProject(UnbindBotFromProjectRequest request, ServerCallContext context)
        => client.UnbindBotFromProjectAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> ActivateBot(ActivateBotRequest request, ServerCallContext context)
        => client.ActivateBotAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> DeactivateBot(DeactivateBotRequest request, ServerCallContext context)
        => client.DeactivateBotAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> UpdateBotToken(UpdateBotTokenRequest request, ServerCallContext context)
        => client.UpdateBotTokenAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> ChangeBotScenarioVersion(ChangeBotScenarioVersionRequest request, ServerCallContext context)
        => client.ChangeBotScenarioVersionAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;

    public override Task<Empty> DeleteBot(DeleteBotRequest request, ServerCallContext context)
        => client.DeleteBotAsync(request, cancellationToken: context.CancellationToken).ResponseAsync;
}
