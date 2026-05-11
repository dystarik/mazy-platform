namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using Grpc.Core;

internal static class GrpcTestMetadata
{
    public static Metadata ForInternal(string token)
    {
        return new Metadata
        {
            { "x-internal-token", token },
            { "x-trace-id", $"it-{Guid.NewGuid():N}" },
        };
    }

    public static Metadata ForUser(Guid userAccountId)
    {
        return new Metadata
        {
            { "x-user-id", userAccountId.ToString() },
            { "x-trace-id", $"it-{Guid.NewGuid():N}" },
        };
    }
}
