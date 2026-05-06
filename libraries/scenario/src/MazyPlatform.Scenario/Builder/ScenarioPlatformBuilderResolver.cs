namespace MazyPlatform.Scenario.Builder;

using MazyPlatform.Scenario.Abstractions.Scenarios;

/// <summary>
/// Резолвит платформенные сборщики графов по ключу платформы.
/// </summary>
public sealed class ScenarioPlatformBuilderResolver(IEnumerable<IScenarioPlatformBuilder> builders) : IScenarioPlatformBuilderResolver
{
    private readonly IReadOnlyDictionary<string, IScenarioPlatformBuilder> _builders = BuildMap(builders);

    /// <inheritdoc />
    public IScenarioPlatformBuilder Resolve(string platformKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformKey);

        var normalizedPlatformKey = platformKey.Trim().ToLowerInvariant();

        if (_builders.TryGetValue(normalizedPlatformKey, out var builder))
        {
            return builder;
        }

        var available = _builders.Count == 0
            ? "<нет>"
            : string.Join(", ", _builders.Keys.Order(StringComparer.Ordinal));

        throw new InvalidOperationException(
            $"Сборщик сценария для платформы \"{normalizedPlatformKey}\" не зарегистрирован. Доступные платформы: {available}.");
    }

    private static IReadOnlyDictionary<string, IScenarioPlatformBuilder> BuildMap(IEnumerable<IScenarioPlatformBuilder> builders)
    {
        var result = new Dictionary<string, IScenarioPlatformBuilder>(StringComparer.Ordinal);

        foreach (var builder in builders)
        {
            var key = builder.PlatformKey.Trim().ToLowerInvariant();

            if (!result.TryAdd(key, builder))
            {
                throw new InvalidOperationException($"Сборщик сценария для платформы \"{key}\" зарегистрирован больше одного раза.");
            }
        }

        return result;
    }
}
