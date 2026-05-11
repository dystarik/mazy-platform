namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using Grpc.Core;

internal static class GrpcAssert
{
    public static async Task<RpcException> ThrowsAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (RpcException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected gRPC call to throw.");
    }
}
