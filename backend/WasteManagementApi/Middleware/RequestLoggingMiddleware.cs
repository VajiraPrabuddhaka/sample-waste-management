namespace WasteManagementApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;

        // Only set response headers before the response starts
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            return Task.CompletedTask;
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        // Guard: don't modify headers if response already committed (e.g. CORS preflight)
        if (!context.Response.HasStarted)
        {
            context.Response.Headers["X-Request-Duration-Ms"] = sw.ElapsedMilliseconds.ToString();
        }

        var statusCode = context.Response.StatusCode;
        var logLevel = statusCode >= 500 ? LogLevel.Error
            : statusCode >= 400 ? LogLevel.Warning
            : LogLevel.Information;

        _logger.Log(logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms [CorrelationId={CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            sw.ElapsedMilliseconds,
            correlationId);
    }
}
