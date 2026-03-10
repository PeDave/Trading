using LaBot.Api.Models.Domain;
using LaBot.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaBot.Api.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionController> _logger;

    private static readonly object[] PricingInfo = new[]
    {
        (object)new { tier = "Free", price = 0, currency = "USD", period = "forever", features = new[] { "Basic market data", "Free signals (delayed)", "Community access" } },
        new { tier = "Pro", price = 29, currency = "USD", period = "month", features = new[] { "All Free features", "Pro signals (real-time)", "Chart analysis tools", "Email notifications" } },
        new { tier = "ProPlus", price = 79, currency = "USD", period = "month", features = new[] { "All Pro features", "Spot & Futures trading", "Auto signal generation", "Priority support", "API access" } }
    };

    public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var tier = await _subscriptionService.GetUserTierAsync(userId);
        return Ok(new { userId, tier = tier.ToString(), tierValue = (int)tier });
    }

    [HttpGet("pricing")]
    [AllowAnonymous]
    public IActionResult GetPricing()
    {
        return Ok(PricingInfo);
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> RequestUpgrade([FromBody] UpgradeRequestDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _logger.LogInformation("User {UserId} requested upgrade to {Tier}", userId, dto.RequestedTier);
        // In production this would integrate with a payment provider (Stripe, etc.)
        // For now we create a pending subscription record and return payment instructions
        return Ok(new
        {
            message = "Upgrade request received. Please complete payment to activate.",
            requestedTier = dto.RequestedTier.ToString(),
            paymentInstructions = "Contact support or visit the billing portal to complete your upgrade.",
            userId
        });
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userTier = await _subscriptionService.GetUserTierAsync(userId);
        var gates = await _subscriptionService.GetFeatureGatesAsync();

        var featureAccess = new List<object>();
        foreach (var gate in gates)
        {
            var accessible = await _subscriptionService.IsFeatureAccessibleAsync(userId, gate.FeatureName);
            featureAccess.Add(new
            {
                featureName = gate.FeatureName,
                isEnabled = gate.IsEnabled,
                accessible,
                requiredTier = gate.MinTier.ToString(),
                delayMinutes = gate.DelayMinutes
            });
        }

        return Ok(new
        {
            currentTier = userTier.ToString(),
            features = featureAccess
        });
    }
}

public class UpgradeRequestDto
{
    public SubscriptionTier RequestedTier { get; set; }
}
