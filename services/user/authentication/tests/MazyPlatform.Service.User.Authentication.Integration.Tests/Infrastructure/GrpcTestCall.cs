namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

internal static class GrpcTestCall
{
    public static DateTime Deadline => DateTime.UtcNow.AddSeconds(10);
}
