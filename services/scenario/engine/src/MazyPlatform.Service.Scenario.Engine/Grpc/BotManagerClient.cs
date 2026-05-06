namespace MazyPlatform.Service.Scenario.Engine.Grpc;

using System.Globalization;
using System.Runtime.CompilerServices;

using global::Grpc.Core;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Configuration.Options;
using MazyPlatform.Service.Scenario.Engine.Observability;
using MazyPlatform.Service.Scenario.Engine.Platforms;

using Microsoft.Extensions.Options;

internal sealed partial class BotManagerClient(
    BotInternalService.BotInternalServiceClient grpcClient,
    IOptions<BotManagerOptions> options,
    ILogger<BotManagerClient> logger) : IBotManagerClient
{
    private readonly string _token = options.Value.AccessToken;

    public async IAsyncEnumerable<BotInstanceCacheEntry> StreamActiveBotsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var platformType in ScenarioPlatformMapper.SupportedGrpcPlatforms)
        {
            var entries = await GetActiveBotsByPlatformAsync(platformType, cancellationToken);

            foreach (var entry in entries)
            {
                yield return entry;
            }
        }
    }

    public async Task<BotInstanceCacheEntry?> GetBotCacheEntryAsync(
        Guid botInstanceId,
        Guid projectId,
        int scenarioVersion,
        PlatformType platformType,
        CancellationToken cancellationToken = default)
    {
        var callOptions = new CallOptions(
            headers: CreateHeaders(),
            cancellationToken: cancellationToken);

        try
        {
            var response = await grpcClient.GetBotCredentialsAsync(
                new GetBotCredentialsRequest { BotInstanceId = botInstanceId.ToString() },
                callOptions);

            var grpcPlatformType = ScenarioPlatformMapper.ToGrpcPlatform(platformType);
            return TryBuildEntry(
                botInstanceId.ToString(),
                projectId.ToString(),
                scenarioVersion,
                grpcPlatformType,
                response.Credentials,
                out var entry)
                ? entry
                : null;
        }
        catch (RpcException ex)
        {
            LogGetCredentialsFailed(ex, botInstanceId);
            return null;
        }
    }

    private async Task<IReadOnlyList<BotInstanceCacheEntry>> GetActiveBotsByPlatformAsync(
        BotPlatformType platformType,
        CancellationToken cancellationToken)
    {
        var entries = new List<BotInstanceCacheEntry>();
        var callOptions = new CallOptions(
            headers: CreateHeaders(),
            cancellationToken: cancellationToken);

        try
        {
            var request = new GetActiveBotsRequest { PlatformType = platformType };
            using var call = grpcClient.GetActiveBots(request, callOptions);

            await foreach (var info in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                if (TryBuildEntry(
                    info.BotInstanceId,
                    info.ProjectId,
                    info.ScenarioVersion,
                    platformType,
                    info.Credentials,
                    out var entry))
                {
                    entries.Add(entry!);
                }
            }
        }
        catch (RpcException ex)
        {
            LogStreamActiveBotsFailed(ex, platformType.ToString());
        }

        return entries;
    }

    private bool TryBuildEntry(
        string botInstanceIdText,
        string projectIdText,
        int scenarioVersion,
        BotPlatformType grpcPlatformType,
        BotCredentials? credentials,
        out BotInstanceCacheEntry? entry)
    {
        entry = null;

        if (!Guid.TryParse(botInstanceIdText, out var botInstanceId)
            || !Guid.TryParse(projectIdText, out var projectId))
        {
            LogInvalidBotInfo(botInstanceIdText);
            return false;
        }

        if (credentials is null)
        {
            LogMissingCredentials(botInstanceIdText, grpcPlatformType.ToString());
            return false;
        }

        var platformType = ScenarioPlatformMapper.ToBotContractPlatform(grpcPlatformType);
        var platformKey = ScenarioPlatformMapper.ToScenarioPlatformKey(platformType);

        if (grpcPlatformType == BotPlatformType.Vk)
        {
            if (credentials.Vk is null)
            {
                LogMissingCredentials(botInstanceIdText, grpcPlatformType.ToString());
                return false;
            }

            if (!int.TryParse(
                credentials.Vk.CommunityId,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var communityId))
            {
                LogInvalidCommunityId(botInstanceIdText, credentials.Vk.CommunityId);
                return false;
            }

            entry = new BotInstanceCacheEntry(
                botInstanceId,
                projectId,
                scenarioVersion,
                platformType,
                platformKey,
                communityId,
                credentials.Vk.AccessToken);
            return true;
        }

        if (platformType == PlatformType.Telegram)
        {
            if (credentials.Telegram is null || string.IsNullOrWhiteSpace(credentials.Telegram.AccessToken))
            {
                LogMissingCredentials(botInstanceIdText, grpcPlatformType.ToString());
                return false;
            }

            entry = new BotInstanceCacheEntry(
                botInstanceId,
                projectId,
                scenarioVersion,
                platformType,
                platformKey,
                null,
                credentials.Telegram.AccessToken);
            return true;
        }

        LogUnsupportedPlatform(grpcPlatformType.ToString());
        return false;
    }

    private Metadata CreateHeaders() => new()
    {
        { "x-internal-token", _token },
        { TraceContext.HeaderName, TraceContext.GetOrCreate() },
    };

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Отсутствуют credentials у бота. BotInstanceId: {BotInstanceId}, Platform: {Platform}.")]
    private partial void LogMissingCredentials(string botInstanceId, string platform);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Некорректные данные бота. BotInstanceId: {BotInstanceId}.")]
    private partial void LogInvalidBotInfo(string botInstanceId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Не удалось получить credentials бота. BotInstanceId: {BotInstanceId}.")]
    private partial void LogGetCredentialsFailed(Exception exception, Guid botInstanceId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Некорректный идентификатор сообщества. BotInstanceId: {BotInstanceId}, CommunityId: {CommunityId}.")]
    private partial void LogInvalidCommunityId(string botInstanceId, string communityId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Не удалось получить активных ботов платформы {Platform}.")]
    private partial void LogStreamActiveBotsFailed(Exception exception, string platform);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Неподдерживаемая платформа активного бота: {Platform}.")]
    private partial void LogUnsupportedPlatform(string platform);
    #endregion
}
