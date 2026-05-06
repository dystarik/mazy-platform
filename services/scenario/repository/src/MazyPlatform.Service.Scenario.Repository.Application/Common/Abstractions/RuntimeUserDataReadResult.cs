namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

public sealed record RuntimeUserDataReadResult(
    IReadOnlyList<RuntimeUserDataRecord> Items,
    int TotalCount);
