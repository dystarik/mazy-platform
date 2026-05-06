namespace MazyPlatform.Scenario.Validation;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;

internal static class PlatformButtonLimitResolver
{
    private static readonly ButtonMatrixLimits InlineSafeLimits = new(6, 5, 10);
    private static readonly ButtonMatrixLimits VkKeyboardLimits = new(10, 5, 40);

    public static IReadOnlyList<NodeParamSchema> Apply(
        string nodeType,
        IReadOnlyList<NodeParamSchema> schema,
        string platformKey) =>
        [.. schema.Select(param => Apply(nodeType, param, platformKey, param.Key))];

    private static NodeParamSchema Apply(
        string nodeType,
        NodeParamSchema param,
        string platformKey,
        string path)
    {
        var fields = param.Fields is null
            ? null
            : param.Fields.Select(field => Apply(nodeType, field, platformKey, $"{path}.{field.Key}")).ToArray();

        var limits = Resolve(nodeType, platformKey, path);

        return param with
        {
            Fields = fields,
            MaxRows = limits?.MaxRows ?? param.MaxRows,
            MaxItemsPerRow = limits?.MaxItemsPerRow ?? param.MaxItemsPerRow,
            MaxItemsTotal = limits?.MaxItemsTotal ?? param.MaxItemsTotal,
        };
    }

    private static ButtonMatrixLimits? Resolve(string nodeType, string platformKey, string path)
    {
        if (string.Equals(path, "buttons", StringComparison.Ordinal)
            && (string.Equals(nodeType, "send_buttons", StringComparison.Ordinal)
                || string.Equals(nodeType, "edit_message", StringComparison.Ordinal)))
        {
            return platformKey switch
            {
                ScenarioPlatformKeys.Telegram => null,
                ScenarioPlatformKeys.Vk => InlineSafeLimits,
                ScenarioPlatformKeys.Universal => InlineSafeLimits,
                _ => InlineSafeLimits,
            };
        }

        if (string.Equals(path, "buttons", StringComparison.Ordinal)
            && string.Equals(nodeType, "vk_send_keyboard", StringComparison.Ordinal)
            && string.Equals(platformKey, ScenarioPlatformKeys.Vk, StringComparison.Ordinal))
        {
            return VkKeyboardLimits;
        }

        return null;
    }
}
