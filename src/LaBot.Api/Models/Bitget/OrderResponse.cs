using System.Text.Json.Serialization;

namespace LaBot.Api.Models.Bitget;

public class OrderResponseData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = "";

    [JsonPropertyName("clientOid")]
    public string ClientOid { get; set; } = "";
}

public class OrderDetailData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "";

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = "";

    [JsonPropertyName("clientOid")]
    public string ClientOid { get; set; } = "";

    [JsonPropertyName("price")]
    public string Price { get; set; } = "";

    [JsonPropertyName("size")]
    public string Size { get; set; } = "";

    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("side")]
    public string Side { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("priceAvg")]
    public string PriceAvg { get; set; } = "";

    [JsonPropertyName("baseVolume")]
    public string BaseVolume { get; set; } = "";

    [JsonPropertyName("quoteVolume")]
    public string QuoteVolume { get; set; } = "";

    [JsonPropertyName("enterPointSource")]
    public string EnterPointSource { get; set; } = "";

    [JsonPropertyName("feeDetail")]
    public string FeeDetail { get; set; } = "";

    [JsonPropertyName("orderSource")]
    public string OrderSource { get; set; } = "";

    [JsonPropertyName("cTime")]
    public string CTime { get; set; } = "";

    [JsonPropertyName("uTime")]
    public string UTime { get; set; } = "";
}

public class FuturesOrderDetailData : OrderDetailData
{
    [JsonPropertyName("marginMode")]
    public string MarginMode { get; set; } = "";

    [JsonPropertyName("tradeSide")]
    public string TradeSide { get; set; } = "";

    [JsonPropertyName("posMode")]
    public string PosMode { get; set; } = "";

    [JsonPropertyName("leverage")]
    public string Leverage { get; set; } = "";

    [JsonPropertyName("marginCoin")]
    public string MarginCoin { get; set; } = "";
}
