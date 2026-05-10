namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

internal static class GrpcTestCall
{
    public static DateTime Deadline => DateTime.UtcNow.AddSeconds(10);
}
