using LaBot.Api.Services;
using System.Security.Claims;

namespace LaBot.Api.Middleware;

public class SubscriptionTierMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionTierMiddleware> _logger;

    // Feature gate mappings by route prefix
    private static readonly Dictionary<string, string> RouteFeatureGates = new()
    {
        { "/api/trading/spot", "spot_trading" },
        { "/api/trading/futures", "futures_trading" },
        { "/api/signals", "signals_access" },
        { "/api/charts", "chart_analysis" },
    };

    public SubscriptionTierMiddleware(RequestDelegate next, ILogger<SubscriptionTierMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
    {
        var path = context.Request.Path.Value ?? "";

        // Only check authenticated requests
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        // Admin bypasses all feature gates
        if (context.User.IsInRole("Admin"))
        {
            await _next(context);
            return;
        }

        // Check if route requires a feature gate
        var matchedGate = RouteFeatureGates
            .FirstOrDefault(kv => path.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(matchedGate.Value))
        {
            var accessible = await subscriptionService.IsFeatureAccessibleAsync(userId, matchedGate.Value);
            if (!accessible)
            {
                _logger.LogInformation("Feature gate '{Feature}' denied for user {UserId} on path {Path}",
                    matchedGate.Value, userId, path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $"{{\"error\":\"Access to this feature requires a higher subscription tier.\",\"feature\":\"{matchedGate.Value}\"}}");
                return;
            }
        }

        await _next(context);
    }
}
