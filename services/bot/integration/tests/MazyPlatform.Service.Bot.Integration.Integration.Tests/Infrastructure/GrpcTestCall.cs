namespace MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

internal static class GrpcTestCall
{
    public static DateTime Deadline => DateTime.UtcNow.AddSeconds(20);
}
