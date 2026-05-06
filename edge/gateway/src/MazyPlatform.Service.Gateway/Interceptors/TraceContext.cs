namespace MazyPlatform.Service.Gateway.Interceptors;

internal static class TraceContext
{
    public const string HeaderName = "x-trace-id";
    public const string ItemKey = "GatewayTraceId";
}
