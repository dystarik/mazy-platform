namespace MazyPlatform.Service.Scenario.Repository.Api.Interceptors;

using System.Diagnostics;

using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using MazyPlatform.Service.Scenario.Repository.Application.Common.Observability;

using Serilog.Context;

internal sealed class TraceIdInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var traceId = GetOrCreateTraceId(context);

        using (TraceContext.BeginScope(traceId))
        using (LogContext.PushProperty("TraceId", traceId))
        {
            context.GetHttpContext().Response.Headers["x-trace-id"] = traceId;
            context.ResponseTrailers.Add("x-trace-id", traceId);

            return await continuation(request, context);
        }
    }

    private static string GetOrCreateTraceId(ServerCallContext context)
    {
        var traceIdFromHeader = context.RequestHeaders
            .FirstOrDefault(h => h.Key.Equals(TraceContext.HeaderName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!string.IsNullOrWhiteSpace(traceIdFromHeader))
            return traceIdFromHeader;

        if (Activity.Current?.TraceId != default)
            return Activity.Current!.TraceId.ToString();

        return Guid.NewGuid().ToString();
    }
}
