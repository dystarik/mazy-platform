namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using Grpc.Core;

internal static class GrpcTestMetadata
{
    public static Metadata ForUser(TestUser user)
    {
        return new Metadata
        {
            { "x-user-id", user.UserAccountId.ToString() },
            { "x-token-id", user.RefreshTokenId.ToString() },
            { "x-trace-id", $"it-{Guid.NewGuid():N}" },
        };
    }

    public static Metadata ForUser(Guid userAccountId, Guid refreshTokenId)
    {
        return new Metadata
        {
            { "x-user-id", userAccountId.ToString() },
            { "x-token-id", refreshTokenId.ToString() },
            { "x-trace-id", $"it-{Guid.NewGuid():N}" },
        };
    }
}
