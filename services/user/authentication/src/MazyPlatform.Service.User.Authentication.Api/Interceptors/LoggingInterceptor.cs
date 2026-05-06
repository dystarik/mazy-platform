namespace MazyPlatform.Service.User.Authentication.Api.Interceptors;

using System.Diagnostics;

using Grpc.Core;
using Grpc.Core.Interceptors;

internal sealed partial class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var method = context.Method;
        var stopwatch = Stopwatch.StartNew();

        RequestStarted("начат", method);
        try
        {
            var response = await continuation(request, context);
            stopwatch.Stop();
            RequestCompleted("завершен", method, context.Status.StatusCode, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (RpcException rpcEx)
        {
            stopwatch.Stop();
            RequestFailedWarning("завершен с ошибкой", rpcEx, method, rpcEx.StatusCode, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RequestFailedError("завершен с необработанной ошибкой", ex, method, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "gRPC запрос {RequestStatus}: {Method}")]
    private partial void RequestStarted(string requestStatus, string method);

    [LoggerMessage(2, LogLevel.Information, "gRPC запрос {RequestStatus}: {Method} | Status: {Status} | Elapsed: {ElapsedMs}ms")]
    private partial void RequestCompleted(string requestStatus, string method, StatusCode status, long elapsedMs);

    [LoggerMessage(3, LogLevel.Warning, "gRPC запрос {RequestStatus}: {Method} | Status: {Status} | Elapsed: {ElapsedMs}ms")]
    private partial void RequestFailedWarning(string requestStatus, RpcException exception, string method, StatusCode status, long elapsedMs);

    [LoggerMessage(4, LogLevel.Error, "gRPC запрос {RequestStatus}: {Method} | Elapsed: {ElapsedMs}ms")]
    private partial void RequestFailedError(string requestStatus, Exception exception, string method, long elapsedMs);
    #endregion
}
