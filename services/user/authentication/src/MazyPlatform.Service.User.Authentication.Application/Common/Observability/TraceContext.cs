namespace MazyPlatform.Service.User.Authentication.Application.Common.Observability;

using System.Diagnostics;

public static class TraceContext
{
    public const string HeaderName = "x-trace-id";

    private static readonly AsyncLocal<string?> CurrentTraceId = new();

    public static IDisposable BeginScope(string traceId)
    {
        var previousTraceId = CurrentTraceId.Value;
        CurrentTraceId.Value = traceId;
        return new TraceScope(previousTraceId);
    }

    public static string GetOrCreate()
    {
        if (!string.IsNullOrWhiteSpace(CurrentTraceId.Value))
            return CurrentTraceId.Value;

        if (Activity.Current?.TraceId != default)
            return Activity.Current!.TraceId.ToString();

        return Guid.NewGuid().ToString();
    }

    private sealed class TraceScope(string? previousTraceId) : IDisposable
    {
        public void Dispose() => CurrentTraceId.Value = previousTraceId;
    }
}
