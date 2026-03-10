using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Domain;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public SubscriptionTier Tier { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
