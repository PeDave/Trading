using LaBot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaBot.Api.Controllers;

[ApiController]
[Route("api/market")]
public class MarketDataController : ControllerBase
{
    private readonly BitgetApiClient _bitgetClient;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(BitgetApiClient bitgetClient, ILogger<MarketDataController> logger)
    {
        _bitgetClient = bitgetClient;
        _logger = logger;
    }

    [HttpGet("tickers")]
    public async Task<IActionResult> GetSpotTickers()
    {
        var result = await _bitgetClient.GetSpotTickersAsync();
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve tickers from Bitget." });
        return Ok(result);
    }

    [HttpGet("ticker/{symbol}")]
    public async Task<IActionResult> GetSpotTicker(string symbol)
    {
        var result = await _bitgetClient.GetSpotTickerAsync(symbol.ToUpperInvariant());
        if (result == null) return NotFound(new { error = $"Ticker '{symbol}' not found." });
        return Ok(result);
    }

    [HttpGet("candles/{symbol}")]
    public async Task<IActionResult> GetSpotCandles(
        string symbol,
        [FromQuery] string granularity = "1H",
        [FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 1000);
        var result = await _bitgetClient.GetSpotCandlesAsync(symbol.ToUpperInvariant(), granularity, limit);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve candle data." });
        return Ok(result);
    }

    [HttpGet("orderbook/{symbol}")]
    public async Task<IActionResult> GetSpotOrderBook(string symbol, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 150);
        var result = await _bitgetClient.GetSpotOrderBookAsync(symbol.ToUpperInvariant(), limit);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve order book." });
        return Ok(result);
    }

    [HttpGet("trades/{symbol}")]
    public async Task<IActionResult> GetSpotTrades(string symbol, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 500);
        var result = await _bitgetClient.GetSpotFillsAsync(symbol.ToUpperInvariant(), limit);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve recent trades." });
        return Ok(result);
    }

    [HttpGet("futures/tickers")]
    public async Task<IActionResult> GetFuturesTickers([FromQuery] string productType = "USDT-FUTURES")
    {
        var result = await _bitgetClient.GetFuturesTickersAsync(productType);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve futures tickers." });
        return Ok(result);
    }

    [HttpGet("futures/candles/{symbol}")]
    public async Task<IActionResult> GetFuturesCandles(
        string symbol,
        [FromQuery] string granularity = "1H",
        [FromQuery] int limit = 100,
        [FromQuery] string productType = "USDT-FUTURES")
    {
        limit = Math.Clamp(limit, 1, 1000);
        var result = await _bitgetClient.GetFuturesCandlesAsync(symbol.ToUpperInvariant(), granularity, limit, productType);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve futures candle data." });
        return Ok(result);
    }

    [HttpGet("futures/depth/{symbol}")]
    public async Task<IActionResult> GetFuturesDepth(string symbol, [FromQuery] int limit = 20, [FromQuery] string productType = "USDT-FUTURES")
    {
        limit = Math.Clamp(limit, 1, 100);
        var result = await _bitgetClient.GetFuturesDepthAsync(symbol.ToUpperInvariant(), limit, productType);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve futures depth." });
        return Ok(result);
    }
}
