using Microsoft.AspNetCore.WebUtilities;

namespace ReferWell.Api.Logging;

/// <summary>
/// Builds a safe request path/query for logs. Never surfaces tokens or credentials.
/// </summary>
public static class RequestLogSanitizer
{
    private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "access_token",
        "token",
        "password",
        "authorization",
        "api_key",
        "apikey",
        "secret"
    };

    public static string SanitizePathAndQuery(PathString path, QueryString query)
    {
        var pathValue = string.IsNullOrEmpty(path.Value) ? "/" : path.Value;
        if (!query.HasValue)
            return pathValue;

        var parsed = QueryHelpers.ParseQuery(query.Value!);
        var parts = new List<string>();
        foreach (var kv in parsed)
        {
            var value = SensitiveQueryKeys.Contains(kv.Key)
                ? "[REDACTED]"
                : kv.Value.ToString();
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(value)}");
        }

        return parts.Count == 0 ? pathValue : $"{pathValue}?{string.Join('&', parts)}";
    }
}
