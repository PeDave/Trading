using Microsoft.AspNetCore.Identity;

namespace LaBot.Api.Models.Domain;

public enum SubscriptionTier
{
    Free = 0,
    Pro = 1,
    ProPlus = 2
}

public class ApplicationUser : IdentityUser
{
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<TradingSignal> CreatedSignals { get; set; } = new List<TradingSignal>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<ChartAnalysis> ChartAnalyses { get; set; } = new List<ChartAnalysis>();
}
