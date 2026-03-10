using System.Text.Json.Serialization;

namespace LaBot.Api.Models.Bitget;

public class CandleData
{
    [JsonPropertyName("ts")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("open")]
    public string Open { get; set; } = "";

    [JsonPropertyName("high")]
    public string High { get; set; } = "";

    [JsonPropertyName("low")]
    public string Low { get; set; } = "";

    [JsonPropertyName("close")]
    public string Close { get; set; } = "";

    [JsonPropertyName("baseVol")]
    public string Volume { get; set; } = "";

    [JsonPropertyName("quoteVol")]
    public string QuoteVolume { get; set; } = "";

    public static CandleData FromArray(string[] arr)
    {
        return new CandleData
        {
            Timestamp = arr.Length > 0 ? arr[0] : "",
            Open = arr.Length > 1 ? arr[1] : "",
            High = arr.Length > 2 ? arr[2] : "",
            Low = arr.Length > 3 ? arr[3] : "",
            Close = arr.Length > 4 ? arr[4] : "",
            Volume = arr.Length > 5 ? arr[5] : "",
            QuoteVolume = arr.Length > 6 ? arr[6] : ""
        };
    }
}
