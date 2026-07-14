using System.Diagnostics;
using System.Security.Claims;
using ReferWell.Api.Logging;

namespace ReferWell.Api.Middleware;

/// <summary>
/// Logs HTTP method, sanitized path, status, duration, and user id.
/// Does not log bodies, Authorization headers, passwords, JWTs, or PHI payloads.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestFileLogger _fileLogger;
    private readonly bool _enabled;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        RequestFileLogger fileLogger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _fileLogger = fileLogger;
        _enabled = configuration.GetValue("RequestLogging:Enabled", true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            WriteLog(context, sw.ElapsedMilliseconds);
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        var p = path.Value ?? string.Empty;
        return p.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || p.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
    }

    private void WriteLog(HttpContext context, long elapsedMs)
    {
        var request = context.Request;
        var path = RequestLogSanitizer.SanitizePathAndQuery(request.Path, request.QueryString);
        var method = request.Method;
        var status = context.Response.StatusCode;
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirstValue("sub")
            ?? "-";
        var traceId = context.TraceIdentifier;

        var line =
            $"{DateTime.UtcNow:O} | {method} {path} | status={status} | {elapsedMs}ms | user={userId} | trace={traceId}";

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms user={UserId} trace={TraceId}",
            method, path, status, elapsedMs, userId, traceId);

        _fileLogger.Enqueue(line);
    }
}
