namespace MazyPlatform.Scenario.Validation;

internal sealed record ButtonMatrixLimits(
    int? MaxRows,
    int? MaxItemsPerRow,
    int? MaxItemsTotal);
