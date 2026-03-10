using System.ComponentModel.DataAnnotations;
using LaBot.Api.Models.Domain;

namespace LaBot.Api.Models.Dto;

public class CreateSignalDto
{
    [Required, MaxLength(20)]
    public string Symbol { get; set; } = "";

    [Required]
    public SignalDirection Direction { get; set; }

    [Required, Range(0.00000001, double.MaxValue)]
    public decimal EntryPrice { get; set; }

    [Required, Range(0.00000001, double.MaxValue)]
    public decimal StopLoss { get; set; }

    [Required, Range(0.00000001, double.MaxValue)]
    public decimal TakeProfit1 { get; set; }

    public decimal? TakeProfit2 { get; set; }
    public decimal? TakeProfit3 { get; set; }

    [Range(0, 100)]
    public int Confidence { get; set; } = 50;

    [MaxLength(10)]
    public string Timeframe { get; set; } = "1h";

    public SubscriptionTier MinTier { get; set; } = SubscriptionTier.Free;

    public DateTime? ExpiresAt { get; set; }

    public SignalSource Source { get; set; } = SignalSource.Manual;

    public string? Notes { get; set; }
}

public class UpdateSignalDto
{
    public SignalStatus? Status { get; set; }
    public decimal? TakeProfit1 { get; set; }
    public decimal? TakeProfit2 { get; set; }
    public decimal? TakeProfit3 { get; set; }
    public decimal? StopLoss { get; set; }
    public int? Confidence { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public class SignalResponseDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit1 { get; set; }
    public decimal? TakeProfit2 { get; set; }
    public decimal? TakeProfit3 { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public int Confidence { get; set; }
    public string Timeframe { get; set; } = "";
    public string Status { get; set; } = "";
    public string MinTier { get; set; } = "";
    public string Source { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string CreatedById { get; set; } = "";
}
