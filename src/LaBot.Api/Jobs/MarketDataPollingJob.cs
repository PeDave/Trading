using LaBot.Api.Hubs;
using LaBot.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace LaBot.Api.Jobs;

public class MarketDataPollingJob
{
    private readonly BitgetApiClient _bitgetClient;
    private readonly IHubContext<SignalHub> _hubContext;
    private readonly ILogger<MarketDataPollingJob> _logger;

    private static readonly string[] TopSymbols = new[]
    {
        "BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT", "XRPUSDT",
        "ADAUSDT", "AVAXUSDT", "DOTUSDT", "LINKUSDT", "MATICUSDT",
        "LTCUSDT", "DOGEUSDT", "UNIUSDT", "ATOMUSDT", "NEARUSDT"
    };

    public MarketDataPollingJob(BitgetApiClient bitgetClient, IHubContext<SignalHub> hubContext, ILogger<MarketDataPollingJob> logger)
    {
        _bitgetClient = bitgetClient;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogDebug("MarketDataPollingJob executing...");

        try
        {
            var tickers = await _bitgetClient.GetSpotTickersAsync();
            if (tickers == null || tickers.Count == 0)
            {
                _logger.LogWarning("No tickers returned from Bitget API");
                return;
            }

            var topTickers = tickers
                .Where(t => TopSymbols.Contains(t.Symbol))
                .Select(t => new
                {
                    symbol = t.Symbol,
                    lastPrice = t.LastPr,
                    priceChangePercent = t.PriceChangePercent,
                    high24h = t.High24h,
                    low24h = t.Low24h,
                    quoteVolume = t.QuoteVolume,
                    baseVolume = t.BaseVolume,
                    ts = t.Ts
                })
                .ToList();

            await _hubContext.Clients.All.SendAsync("MarketDataUpdate", topTickers);
            _logger.LogDebug("Market data pushed for {Count} symbols", topTickers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MarketDataPollingJob");
        }
    }
}
