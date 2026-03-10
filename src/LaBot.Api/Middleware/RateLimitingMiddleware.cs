using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;

namespace LaBot.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    private const int AnonymousRequestsPerMinute = 100;
    private const int AuthenticatedRequestsPerMinute = 500;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for Hangfire dashboard and health checks
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/hangfire") || path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        var key = GetRateLimitKey(context);
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var limit = isAuthenticated ? AuthenticatedRequestsPerMinute : AnonymousRequestsPerMinute;

        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(limit));

        // Clean up old buckets periodically
        CleanupOldBuckets();

        if (!bucket.TryConsume())
        {
            _logger.LogWarning("Rate limit exceeded for {Key} (authenticated={IsAuth})", key, isAuthenticated);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded. Please try again later.\"}");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = bucket.Remaining.ToString();

        await _next(context);
    }

    private static string GetRateLimitKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    private static DateTime _lastCleanup = DateTime.UtcNow;

    private static void CleanupOldBuckets()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastCleanup).TotalMinutes < 5) return;
        _lastCleanup = now;

        var stale = _buckets
            .Where(kv => kv.Value.IsStale)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in stale)
            _buckets.TryRemove(key, out _);
    }
}

public class TokenBucket
{
    private readonly int _capacity;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();

    public TokenBucket(int requestsPerMinute)
    {
        _capacity = requestsPerMinute;
        _tokens = requestsPerMinute;
        _lastRefill = DateTime.UtcNow;
    }

    public int Remaining
    {
        get
        {
            lock (_lock)
            {
                Refill();
                return (int)_tokens;
            }
        }
    }

    public bool IsStale
    {
        get
        {
            lock (_lock)
            {
                return (DateTime.UtcNow - _lastRefill).TotalMinutes > 10;
            }
        }
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            Refill();
            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }
            return false;
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalMinutes;
        if (elapsed > 0)
        {
            _tokens = Math.Min(_capacity, _tokens + elapsed * _capacity);
            _lastRefill = now;
        }
    }
}
