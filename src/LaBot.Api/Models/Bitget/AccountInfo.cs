using System.Text.Json.Serialization;

namespace LaBot.Api.Models.Bitget;

public class SpotAccountAsset
{
    [JsonPropertyName("coinId")]
    public string CoinId { get; set; } = "";

    [JsonPropertyName("coinName")]
    public string CoinName { get; set; } = "";

    [JsonPropertyName("available")]
    public string Available { get; set; } = "";

    [JsonPropertyName("frozen")]
    public string Frozen { get; set; } = "";

    [JsonPropertyName("locked")]
    public string Locked { get; set; } = "";

    [JsonPropertyName("uTime")]
    public string UTime { get; set; } = "";
}

public class FuturesAccountData
{
    [JsonPropertyName("marginCoin")]
    public string MarginCoin { get; set; } = "";

    [JsonPropertyName("locked")]
    public string Locked { get; set; } = "";

    [JsonPropertyName("available")]
    public string Available { get; set; } = "";

    [JsonPropertyName("crossedMaxAvailable")]
    public string CrossedMaxAvailable { get; set; } = "";

    [JsonPropertyName("isolatedMaxAvailable")]
    public string IsolatedMaxAvailable { get; set; } = "";

    [JsonPropertyName("maxTransferOut")]
    public string MaxTransferOut { get; set; } = "";

    [JsonPropertyName("equity")]
    public string Equity { get; set; } = "";

    [JsonPropertyName("usdtEquity")]
    public string UsdtEquity { get; set; } = "";

    [JsonPropertyName("btcEquity")]
    public string BtcEquity { get; set; } = "";

    [JsonPropertyName("crossedRiskRate")]
    public string CrossedRiskRate { get; set; } = "";

    [JsonPropertyName("crossedUnrealizedPL")]
    public string CrossedUnrealizedPL { get; set; } = "";

    [JsonPropertyName("isolatedUnrealizedPL")]
    public string IsolatedUnrealizedPL { get; set; } = "";

    [JsonPropertyName("unrealizedPL")]
    public string UnrealizedPL { get; set; } = "";

    [JsonPropertyName("coupon")]
    public string Coupon { get; set; } = "";
}
