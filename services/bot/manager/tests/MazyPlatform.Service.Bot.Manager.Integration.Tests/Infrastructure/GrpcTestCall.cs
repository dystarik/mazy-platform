namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

internal static class GrpcTestCall
{
    public static DateTime Deadline => DateTime.UtcNow.AddSeconds(10);
}
