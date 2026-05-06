namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

public interface IRuntimeUserDataReader
{
    Task<RuntimeUserDataReadResult> GetRecordsAsync(
        RuntimeUserDataReadRequest request,
        CancellationToken cancellationToken = default);
}
