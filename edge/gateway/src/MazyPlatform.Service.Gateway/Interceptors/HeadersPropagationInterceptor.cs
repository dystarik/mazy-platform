namespace MazyPlatform.Service.Gateway.Interceptors;

using Grpc.Core;
using Grpc.Core.Interceptors;

internal sealed class HeadersPropagationInterceptor(IHttpContextAccessor accessor) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = context.Options.Headers ?? new Metadata();
        var http = accessor.HttpContext;

        if (http is null)
            return continuation(request, context);

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (ip is not null)
            headers.Add("x-forwarded-for", ip);

        var traceId = GetTraceId(http);
        if (traceId is not null)
            headers.Add(TraceContext.HeaderName, traceId);

        var userId = http.User.FindFirst("sub")?.Value;
        if (userId is not null)
            headers.Add("x-user-id", userId);

        var jti = http.User.FindFirst("jti")?.Value;
        if (jti is not null)
            headers.Add("x-token-id", jti);

        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return continuation(request, newContext);
    }

    private static string? GetTraceId(HttpContext http)
    {
        if (http.Items.TryGetValue(TraceContext.ItemKey, out var traceId) && traceId is string value)
            return value;

        return http.Request.Headers[TraceContext.HeaderName].FirstOrDefault();
    }
}
