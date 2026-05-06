namespace MazyPlatform.Service.Bot.Manager.Api.Services;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Bot.Manager.Api.Common.Mapping;
using MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;
using MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;
using MazyPlatform.SharedKernel.Api.Extensions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

using DomainScenarioVersionUpdateMode = MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects.ScenarioVersionUpdateMode;

internal sealed class BotGrpcService(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : BotService.BotServiceBase
{
    public override async Task<CreateBotResponse> CreateBot(CreateBotRequest request, ServerCallContext context)
    {
        var scenarioVersionUpdateMode = request.HasScenarioVersionUpdateMode
            ? request.ScenarioVersionUpdateMode.ToDomainOrDefault()
            : DomainScenarioVersionUpdateMode.Auto;

        var command = new CreateBotCommand(
            context.GetUserAccountId(),
            request.ProjectId,
            request.Name,
            request.PlatformType.ToDomain(),
            request.AccessToken,
            request.HasCommunityId ? request.CommunityId : null,
            request.ScenarioVersion,
            scenarioVersionUpdateMode);

        var result = await commandDispatcher.DispatchAsync<CreateBotCommand, CreateBotResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new CreateBotResponse { BotInstanceId = r.BotInstanceId.ToString() });
    }

    public override async Task<CreateBotResponse> CreateBotWithoutProject(CreateBotWithoutProjectRequest request, ServerCallContext context)
    {
        var scenarioVersionUpdateMode = request.HasScenarioVersionUpdateMode
            ? request.ScenarioVersionUpdateMode.ToDomainOrDefault()
            : DomainScenarioVersionUpdateMode.Auto;

        var command = new CreateBotWithoutProjectCommand(
            context.GetUserAccountId(),
            request.Name,
            request.PlatformType.ToDomain(),
            request.AccessToken,
            request.HasCommunityId ? request.CommunityId : null,
            scenarioVersionUpdateMode);

        var result = await commandDispatcher.DispatchAsync<CreateBotWithoutProjectCommand, CreateBotResult>(command, context.CancellationToken);
        return result.ToGrpcResponse(r => new CreateBotResponse { BotInstanceId = r.BotInstanceId.ToString() });
    }

    public override async Task<GetBotResponse> GetBot(GetBotRequest request, ServerCallContext context)
    {
        var query = new GetBotQuery(request.BotInstanceId, context.GetUserAccountId());

        var result = await queryDispatcher.DispatchAsync<GetBotQuery, GetBotResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetBotResponse
            {
                BotInstanceId = r.BotInstanceId.ToString(),
                Name = r.Name,
                PlatformType = r.PlatformType.ToProto(),
                MaskedAccessToken = r.MaskedAccessToken,
                Status = r.Status.ToProto(),
                ScenarioVersionUpdateMode = r.ScenarioVersionUpdateMode.ToProto(),
            };

            if (r.ProjectId.HasValue)
                response.ProjectId = r.ProjectId.Value.ToString();

            if (r.CommunityId is not null)
                response.CommunityId = r.CommunityId;

            if (r.ScenarioVersion.HasValue)
                response.ScenarioVersion = r.ScenarioVersion.Value;

            return response;
        });
    }

    public override async Task<GetBotsByProjectResponse> GetBotsByProject(GetBotsByProjectRequest request, ServerCallContext context)
    {
        var query = new GetBotsByProjectQuery(request.ProjectId, context.GetUserAccountId());

        var result = await queryDispatcher.DispatchAsync<GetBotsByProjectQuery, GetBotsByProjectResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetBotsByProjectResponse();
            foreach (var item in r.Items)
            {
                var protoItem = new BotListItem
                {
                    BotInstanceId = item.BotInstanceId.ToString(),
                    Name = item.Name,
                    PlatformType = item.PlatformType.ToProto(),
                    MaskedAccessToken = item.MaskedAccessToken,
                    Status = item.Status.ToProto(),
                    ScenarioVersionUpdateMode = item.ScenarioVersionUpdateMode.ToProto(),
                };

                if (item.CommunityId is not null)
                    protoItem.CommunityId = item.CommunityId;

                if (item.ScenarioVersion.HasValue)
                    protoItem.ScenarioVersion = item.ScenarioVersion.Value;

                response.Items.Add(protoItem);
            }

            return response;
        });
    }

    public override async Task<GetBotsByUserIdResponse> GetBotsByUserId(Empty request, ServerCallContext context)
    {
        var query = new GetBotsByUserIdQuery(context.GetUserAccountId());

        var result = await queryDispatcher.DispatchAsync<GetBotsByUserIdQuery, GetBotsByUserIdResult>(query, context.CancellationToken);
        return result.ToGrpcResponse(r =>
        {
            var response = new GetBotsByUserIdResponse();
            foreach (var item in r.Items)
            {
                var protoItem = new BotListItem
                {
                    BotInstanceId = item.BotInstanceId.ToString(),
                    Name = item.Name,
                    PlatformType = item.PlatformType.ToProto(),
                    MaskedAccessToken = item.MaskedAccessToken,
                    Status = item.Status.ToProto(),
                    ScenarioVersionUpdateMode = item.ScenarioVersionUpdateMode.ToProto(),
                };

                if (item.CommunityId is not null)
                    protoItem.CommunityId = item.CommunityId;

                if (item.ScenarioVersion.HasValue)
                    protoItem.ScenarioVersion = item.ScenarioVersion.Value;

                if (item.ProjectId.HasValue)
                    protoItem.ProjectId = item.ProjectId.Value.ToString();

                response.Items.Add(protoItem);
            }

            return response;
        });
    }

    public override async Task<Empty> BindBotToProject(BindBotToProjectRequest request, ServerCallContext context)
    {
        var scenarioVersionUpdateMode = request.HasScenarioVersionUpdateMode
            ? request.ScenarioVersionUpdateMode.ToDomainOrDefault()
            : DomainScenarioVersionUpdateMode.Auto;

        var command = new BindBotToProjectCommand(
            request.BotInstanceId,
            context.GetUserAccountId(),
            request.ProjectId,
            request.ScenarioVersion,
            scenarioVersionUpdateMode);

        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> UnbindBotFromProject(UnbindBotFromProjectRequest request, ServerCallContext context)
    {
        var command = new UnbindBotFromProjectCommand(request.BotInstanceId, context.GetUserAccountId());
        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> ActivateBot(ActivateBotRequest request, ServerCallContext context)
    {
        var command = new ActivateBotCommand(request.BotInstanceId, context.GetUserAccountId());
        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> DeactivateBot(DeactivateBotRequest request, ServerCallContext context)
    {
        var command = new DeactivateBotCommand(request.BotInstanceId, context.GetUserAccountId());
        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> UpdateBotToken(UpdateBotTokenRequest request, ServerCallContext context)
    {
        var command = new UpdateBotTokenCommand(
            request.BotInstanceId,
            context.GetUserAccountId(),
            request.AccessToken,
            request.HasCommunityId ? request.CommunityId : null);

        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> ChangeBotScenarioVersion(ChangeBotScenarioVersionRequest request, ServerCallContext context)
    {
        var command = new ChangeBotScenarioVersionCommand(
            request.BotInstanceId,
            context.GetUserAccountId(),
            request.NewScenarioVersion);

        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> ChangeBotScenarioVersionUpdateMode(ChangeBotScenarioVersionUpdateModeRequest request, ServerCallContext context)
    {
        var command = new ChangeBotScenarioVersionUpdateModeCommand(
            request.BotInstanceId,
            context.GetUserAccountId(),
            request.ScenarioVersionUpdateMode.ToDomainOrDefault());

        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }

    public override async Task<Empty> DeleteBot(DeleteBotRequest request, ServerCallContext context)
    {
        var command = new DeleteBotCommand(request.BotInstanceId, context.GetUserAccountId());
        var result = await commandDispatcher.DispatchAsync(command, context.CancellationToken);
        return result.ToGrpcResponse();
    }
}
