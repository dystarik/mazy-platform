namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.Grpc;

using global::Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Application.Common.Observability;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using DomainPlatformType = MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects.PlatformType;
using ProtoPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

internal sealed partial class ScenarioRepositoryClient(
    ScenarioRepositoryInternalService.ScenarioRepositoryInternalServiceClient grpcClient,
    IOptions<ScenarioRepositoryClientOptions> options,
    ILogger<ScenarioRepositoryClient> logger) : IScenarioRepositoryClient
{
    private readonly string _token = options.Value.AccessToken;

    public async Task<Result> ValidateProjectForBotAsync(
        Guid ownerAccountId,
        Guid projectId,
        DomainPlatformType platformType,
        int? scenarioVersion,
        CancellationToken cancellationToken = default)
    {
        if (scenarioVersion is null)
            return Result.Success();

        var callOptions = new CallOptions(
            headers: new Metadata
            {
                { "x-internal-token", _token },
                { TraceContext.HeaderName, TraceContext.GetOrCreate() },
            },
            cancellationToken: cancellationToken);

        try
        {
            await grpcClient.ValidateProjectForBotAsync(
                new ValidateProjectForBotRequest
                {
                    ProjectId = projectId.ToString(),
                    OwnerAccountId = ownerAccountId.ToString(),
                    PlatformType = ToProto(platformType),
                    ScenarioVersion = scenarioVersion.Value,
                },
                callOptions);

            return Result.Success();
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            ProjectOrVersionNotFound(projectId, e.Status.Detail);
            return Error.NotFound(ErrorCodes.BotInstance.ProjectNotFound, e.Status.Detail);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
        {
            PlatformMismatch(projectId, e.Status.Detail);
            return Error.Conflict(ErrorCodes.BotInstance.PlatformMismatch, e.Status.Detail);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.Unauthenticated)
        {
            AccessDenied(projectId, e.Status.Detail);
            return Error.Unauthorized(ErrorCodes.BotInstance.AccessDenied, e.Status.Detail);
        }
    }

    private static ProtoPlatformType ToProto(DomainPlatformType platformType) => platformType switch
    {
        DomainPlatformType.Vk => ProtoPlatformType.Vk,
        DomainPlatformType.Telegram => ProtoPlatformType.Telegram,
        _ => throw new ArgumentException($"Неподдерживаемый тип платформы: {platformType}", nameof(platformType)),
    };

    #region Logging
    [LoggerMessage(1, LogLevel.Warning,
        "ValidateProjectForBot — проект или версия не найдены. ProjectId={ProjectId}. Detail={Detail}.")]
    private partial void ProjectOrVersionNotFound(Guid projectId, string detail);

    [LoggerMessage(2, LogLevel.Warning,
        "ValidateProjectForBot — несовместимость платформы. ProjectId={ProjectId}. Detail={Detail}.")]
    private partial void PlatformMismatch(Guid projectId, string detail);

    [LoggerMessage(3, LogLevel.Warning,
        "ValidateProjectForBot — нет доступа к проекту. ProjectId={ProjectId}. Detail={Detail}.")]
    private partial void AccessDenied(Guid projectId, string detail);
    #endregion
}
