using LaBot.Api.Data;
using LaBot.Api.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaBot.Api.Services;

public interface ISubscriptionService
{
    Task<SubscriptionTier> GetUserTierAsync(string userId);
    Task<bool> UpdateUserTierAsync(string userId, SubscriptionTier tier);
    Task<bool> IsFeatureAccessibleAsync(string userId, string featureName);
    Task<List<FeatureGate>> GetFeatureGatesAsync();
    Task<FeatureGate> UpsertFeatureGateAsync(FeatureGate gate);
    Task<bool> DeleteFeatureGateAsync(Guid id);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext db, UserManager<ApplicationUser> userManager, ILogger<SubscriptionService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<SubscriptionTier> GetUserTierAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.SubscriptionTier ?? SubscriptionTier.Free;
    }

    public async Task<bool> UpdateUserTierAsync(string userId, SubscriptionTier tier)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.SubscriptionTier = tier;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to update tier for user {UserId}: {Errors}", userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        // Create subscription record
        var existingActive = await _db.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();
        foreach (var sub in existingActive)
        {
            sub.IsActive = false;
        }

        if (tier != SubscriptionTier.Free)
        {
            _db.Subscriptions.Add(new Subscription
            {
                UserId = userId,
                Tier = tier,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsFeatureAccessibleAsync(string userId, string featureName)
    {
        var gate = await _db.FeatureGates
            .FirstOrDefaultAsync(f => f.FeatureName == featureName);

        if (gate == null) return true;
        if (!gate.IsEnabled) return false;

        // Check schedule window
        var now = DateTime.UtcNow;
        if (gate.ScheduleStart.HasValue && now < gate.ScheduleStart.Value) return false;
        if (gate.ScheduleEnd.HasValue && now > gate.ScheduleEnd.Value) return false;

        var userTier = await GetUserTierAsync(userId);

        // DelayMinutes: lower-tier users get delayed access (e.g., Free sees signals 15 minutes late)
        // If the user meets the required tier exactly (not above), apply the delay check against UpdatedAt
        if (gate.DelayMinutes > 0 && userTier == gate.MinTier)
        {
            // Access is gated: the feature gate was last updated/enabled DelayMinutes ago
            var accessibleAfter = gate.UpdatedAt.AddMinutes(gate.DelayMinutes);
            if (now < accessibleAfter) return false;
        }

        // Users below the minimum tier never get access
        if (userTier < gate.MinTier) return false;

        return true;
    }

    public async Task<List<FeatureGate>> GetFeatureGatesAsync()
    {
        return await _db.FeatureGates
            .OrderBy(f => f.FeatureName)
            .ToListAsync();
    }

    public async Task<FeatureGate> UpsertFeatureGateAsync(FeatureGate gate)
    {
        var existing = await _db.FeatureGates
            .FirstOrDefaultAsync(f => f.FeatureName == gate.FeatureName);

        if (existing == null)
        {
            gate.Id = Guid.NewGuid();
            gate.UpdatedAt = DateTime.UtcNow;
            _db.FeatureGates.Add(gate);
        }
        else
        {
            existing.MinTier = gate.MinTier;
            existing.IsEnabled = gate.IsEnabled;
            existing.DelayMinutes = gate.DelayMinutes;
            existing.ScheduleStart = gate.ScheduleStart;
            existing.ScheduleEnd = gate.ScheduleEnd;
            existing.UpdatedBy = gate.UpdatedBy;
            existing.UpdatedAt = DateTime.UtcNow;
            gate = existing;
        }

        await _db.SaveChangesAsync();
        return gate;
    }

    public async Task<bool> DeleteFeatureGateAsync(Guid id)
    {
        var gate = await _db.FeatureGates.FindAsync(id);
        if (gate == null) return false;

        _db.FeatureGates.Remove(gate);
        await _db.SaveChangesAsync();
        return true;
    }
}
