using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LaBot.Api.Models.Bitget;

public class PlaceSpotOrderRequest
{
    [JsonPropertyName("symbol")]
    [Required]
    public string Symbol { get; set; } = "";

    [JsonPropertyName("side")]
    [Required]
    public string Side { get; set; } = "";

    [JsonPropertyName("orderType")]
    [Required]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("force")]
    public string Force { get; set; } = "gtc";

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("size")]
    [Required]
    public string Size { get; set; } = "";

    [JsonPropertyName("clientOid")]
    public string? ClientOid { get; set; }
}

public class PlaceFuturesOrderRequest
{
    [JsonPropertyName("symbol")]
    [Required]
    public string Symbol { get; set; } = "";

    [JsonPropertyName("productType")]
    public string ProductType { get; set; } = "USDT-FUTURES";

    [JsonPropertyName("marginMode")]
    public string MarginMode { get; set; } = "crossed";

    [JsonPropertyName("marginCoin")]
    public string MarginCoin { get; set; } = "USDT";

    [JsonPropertyName("size")]
    [Required]
    public string Size { get; set; } = "";

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("side")]
    [Required]
    public string Side { get; set; } = "";

    [JsonPropertyName("tradeSide")]
    public string TradeSide { get; set; } = "open";

    [JsonPropertyName("orderType")]
    [Required]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("force")]
    public string Force { get; set; } = "gtc";

    [JsonPropertyName("clientOid")]
    public string? ClientOid { get; set; }

    [JsonPropertyName("presetStopSurplusPrice")]
    public string? PresetStopSurplusPrice { get; set; }

    [JsonPropertyName("presetStopLossPrice")]
    public string? PresetStopLossPrice { get; set; }
}

public class CancelOrderRequest
{
    [JsonPropertyName("symbol")]
    [Required]
    public string Symbol { get; set; } = "";

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("clientOid")]
    public string? ClientOid { get; set; }
}
