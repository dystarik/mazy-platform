namespace MazyPlatform.Scenario.Helpers;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;

internal static class ButtonVariableResolver
{
    public static ButtonLayout ResolveLabels(
        ExecutionContext context,
        ButtonLayout buttons) =>
        new(
            [.. buttons.Rows.Select(row => ResolveRow(context, row))]);

    private static IReadOnlyList<ButtonDefinition> ResolveRow(
        ExecutionContext context,
        IReadOnlyList<ButtonDefinition> row) =>
        [.. row.Select(button => new ButtonDefinition(
            context.ResolveVariables(button.Label),
            button.Payload,
            button.Style))];
}
