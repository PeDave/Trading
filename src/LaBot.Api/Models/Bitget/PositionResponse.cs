using System.Text.Json.Serialization;

namespace LaBot.Api.Models.Bitget;

public class FuturesPositionData
{
    [JsonPropertyName("marginCoin")]
    public string MarginCoin { get; set; } = "";

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "";

    [JsonPropertyName("holdSide")]
    public string HoldSide { get; set; } = "";

    [JsonPropertyName("openDelegateSize")]
    public string OpenDelegateSize { get; set; } = "";

    [JsonPropertyName("marginSize")]
    public string MarginSize { get; set; } = "";

    [JsonPropertyName("available")]
    public string Available { get; set; } = "";

    [JsonPropertyName("locked")]
    public string Locked { get; set; } = "";

    [JsonPropertyName("total")]
    public string Total { get; set; } = "";

    [JsonPropertyName("leverage")]
    public string Leverage { get; set; } = "";

    [JsonPropertyName("achievedProfits")]
    public string AchievedProfits { get; set; } = "";

    [JsonPropertyName("openPriceAvg")]
    public string OpenPriceAvg { get; set; } = "";

    [JsonPropertyName("marginMode")]
    public string MarginMode { get; set; } = "";

    [JsonPropertyName("posMode")]
    public string PosMode { get; set; } = "";

    [JsonPropertyName("unrealizedPL")]
    public string UnrealizedPL { get; set; } = "";

    [JsonPropertyName("liquidationPrice")]
    public string LiquidationPrice { get; set; } = "";

    [JsonPropertyName("keepMarginRate")]
    public string KeepMarginRate { get; set; } = "";

    [JsonPropertyName("markPrice")]
    public string MarkPrice { get; set; } = "";

    [JsonPropertyName("breakEvenPrice")]
    public string BreakEvenPrice { get; set; } = "";

    [JsonPropertyName("totalFee")]
    public string TotalFee { get; set; } = "";

    [JsonPropertyName("deductedFee")]
    public string DeductedFee { get; set; } = "";

    [JsonPropertyName("marginRatio")]
    public string MarginRatio { get; set; } = "";

    [JsonPropertyName("cTime")]
    public string CTime { get; set; } = "";

    [JsonPropertyName("uTime")]
    public string UTime { get; set; } = "";

    [JsonPropertyName("autoMargin")]
    public string AutoMargin { get; set; } = "";
}
