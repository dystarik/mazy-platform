namespace MazyPlatform.Service.Gateway.Interceptors;

using System.Diagnostics;

using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;

using Serilog.Context;

internal sealed class TraceIdInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var httpContext = context.GetHttpContext();
        var traceId = GetOrCreateTraceId(context, httpContext);

        httpContext.Items[TraceContext.ItemKey] = traceId;
        httpContext.Response.Headers[TraceContext.HeaderName] = traceId;
        context.ResponseTrailers.Add(TraceContext.HeaderName, traceId);

        using (LogContext.PushProperty("TraceId", traceId))
            return await continuation(request, context);
    }

    private static string GetOrCreateTraceId(ServerCallContext context, HttpContext httpContext)
    {
        var traceIdFromHeader = context.RequestHeaders
            .FirstOrDefault(h => h.Key.Equals(TraceContext.HeaderName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!string.IsNullOrWhiteSpace(traceIdFromHeader))
            return traceIdFromHeader;

        traceIdFromHeader = httpContext.Request.Headers[TraceContext.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(traceIdFromHeader))
            return traceIdFromHeader;

        if (Activity.Current?.TraceId != default)
            return Activity.Current!.TraceId.ToString();

        return Guid.NewGuid().ToString();
    }
}
