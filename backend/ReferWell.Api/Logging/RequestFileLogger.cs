using System.Collections.Concurrent;
using System.Text;

namespace ReferWell.Api.Logging;

/// <summary>
/// Appends request log lines to dated files under the configured logs directory.
/// </summary>
public sealed class RequestFileLogger : IDisposable
{
    private readonly string _logDirectory;
    private readonly ILogger<RequestFileLogger> _logger;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly Timer _flushTimer;
    private bool _disposed;

    public RequestFileLogger(IConfiguration configuration, IHostEnvironment env, ILogger<RequestFileLogger> logger)
    {
        _logger = logger;
        var relative = configuration["RequestLogging:LogDirectory"] ?? "../logs";
        _logDirectory = Path.GetFullPath(Path.Combine(env.ContentRootPath, relative));
        Directory.CreateDirectory(_logDirectory);
        _flushTimer = new Timer(_ => _ = FlushAsync(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public void Enqueue(string line)
    {
        if (_disposed) return;
        _queue.Enqueue(line);
        if (_queue.Count >= 32)
            _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        if (_disposed || _queue.IsEmpty) return;
        if (!await _flushLock.WaitAsync(0).ConfigureAwait(false))
            return;

        try
        {
            if (_queue.IsEmpty) return;

            var path = Path.Combine(_logDirectory, $"requests-{DateTime.UtcNow:yyyy-MM-dd}.log");
            var sb = new StringBuilder();
            while (_queue.TryDequeue(out var line))
                sb.AppendLine(line);

            if (sb.Length == 0) return;
            await File.AppendAllTextAsync(path, sb.ToString()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write request log file under {LogDirectory}", _logDirectory);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _flushTimer.Dispose();
        FlushAsync().GetAwaiter().GetResult();
        _flushLock.Dispose();
    }
}
