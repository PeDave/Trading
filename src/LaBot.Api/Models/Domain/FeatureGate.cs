using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Domain;

public class FeatureGate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string FeatureName { get; set; } = "";

    public SubscriptionTier MinTier { get; set; } = SubscriptionTier.Free;

    public bool IsEnabled { get; set; } = true;

    public int DelayMinutes { get; set; } = 0;

    public DateTime? ScheduleStart { get; set; }
    public DateTime? ScheduleEnd { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
