using System.Text.Json.Serialization;

namespace LaBot.Api.Models.Bitget;

public class BitgetResponse<T>
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "00000";

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = "success";

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("requestTime")]
    public long RequestTime { get; set; }

    public bool IsSuccess => Code == "00000";
}

public class SpotTickerData
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "";

    [JsonPropertyName("lastPr")]
    public string LastPr { get; set; } = "";

    [JsonPropertyName("open24h")]
    public string Open24h { get; set; } = "";

    [JsonPropertyName("high24h")]
    public string High24h { get; set; } = "";

    [JsonPropertyName("low24h")]
    public string Low24h { get; set; } = "";

    [JsonPropertyName("quoteVolume")]
    public string QuoteVolume { get; set; } = "";

    [JsonPropertyName("baseVolume")]
    public string BaseVolume { get; set; } = "";

    [JsonPropertyName("priceChangePercent")]
    public string PriceChangePercent { get; set; } = "";

    [JsonPropertyName("ts")]
    public string Ts { get; set; } = "";

    [JsonPropertyName("bidPr")]
    public string BidPr { get; set; } = "";

    [JsonPropertyName("askPr")]
    public string AskPr { get; set; } = "";

    [JsonPropertyName("openUtc")]
    public string OpenUtc { get; set; } = "";

    [JsonPropertyName("changeUtc24h")]
    public string ChangeUtc24h { get; set; } = "";

    [JsonPropertyName("change24h")]
    public string Change24h { get; set; } = "";
}
