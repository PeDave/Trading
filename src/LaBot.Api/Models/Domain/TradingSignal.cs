using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaBot.Api.Models.Domain;

public enum SignalDirection
{
    Long,
    Short
}

public enum SignalStatus
{
    Active,
    TP1Hit,
    TP2Hit,
    TP3Hit,
    StopLoss,
    Expired,
    Cancelled
}

public enum SignalSource
{
    Manual,
    SemiAuto,
    Auto
}

public class TradingSignal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(20)]
    public string Symbol { get; set; } = "";

    public SignalDirection Direction { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal EntryPrice { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal StopLoss { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal TakeProfit1 { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal? TakeProfit2 { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal? TakeProfit3 { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RiskRewardRatio { get; set; }

    [Range(0, 100)]
    public int Confidence { get; set; }

    [MaxLength(10)]
    public string Timeframe { get; set; } = "1h";

    public SignalStatus Status { get; set; } = SignalStatus.Active;

    public SubscriptionTier MinTier { get; set; } = SubscriptionTier.Free;

    public string CreatedById { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public SignalSource Source { get; set; } = SignalSource.Manual;

    public string? Notes { get; set; }
}
