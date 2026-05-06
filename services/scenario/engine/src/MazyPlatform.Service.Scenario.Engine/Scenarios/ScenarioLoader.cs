namespace MazyPlatform.Service.Scenario.Engine.Scenarios;

using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Service.Scenario.Engine.Caching;
using MazyPlatform.Service.Scenario.Engine.Grpc;

internal sealed partial class ScenarioLoader(
    ScenarioCache cache,
    IScenarioRepositoryClient repositoryClient,
    IScenarioPlatformBuilderResolver builderResolver,
    ILogger<ScenarioLoader> logger)
{
    public async Task<ScenarioGraph?> GetOrLoadAsync(Guid projectId, int version, string platformKey, CancellationToken cancellationToken = default)
    {
        if (cache.TryGet(projectId, version, platformKey, out var cached))
            return cached;

        LogLoadingScenario(projectId, version, platformKey);

        var json = await repositoryClient.GetScenarioJsonAsync(projectId, version, cancellationToken);
        if (json is null)
        {
            LogScenarioNotFound(projectId, version, platformKey);
            return null;
        }

        var builder = builderResolver.Resolve(platformKey);
        var graph = builder.Build(json);
        cache.Set(projectId, version, platformKey, graph);

        LogScenarioLoaded(projectId, version, platformKey);
        return graph;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Загрузка сценария из репозитория. ProjectId: {ProjectId}, Version: {Version}, Platform: {PlatformKey}.")]
    private partial void LogLoadingScenario(Guid projectId, int version, string platformKey);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Сценарий не найден в репозитории. ProjectId: {ProjectId}, Version: {Version}, Platform: {PlatformKey}.")]
    private partial void LogScenarioNotFound(Guid projectId, int version, string platformKey);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information,
        Message = "Сценарий загружен и закэширован. ProjectId: {ProjectId}, Version: {Version}, Platform: {PlatformKey}.")]
    private partial void LogScenarioLoaded(Guid projectId, int version, string platformKey);
}
