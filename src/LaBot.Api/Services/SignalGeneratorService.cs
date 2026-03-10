using LaBot.Api.Data;
using LaBot.Api.Models.Bitget;
using LaBot.Api.Models.Domain;
using LaBot.Api.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace LaBot.Api.Services;

public interface ISignalGeneratorService
{
    Task<List<TradingSignal>> EvaluateSignalsAsync();
    Task<TradingSignal?> CreateSignalAsync(CreateSignalDto dto, string createdById);
    Task<bool> UpdateSignalStatusAsync(Guid signalId, SignalStatus status);
}

public class SignalGeneratorService : ISignalGeneratorService
{
    private readonly AppDbContext _db;
    private readonly BitgetApiClient _bitgetClient;
    private readonly ILogger<SignalGeneratorService> _logger;

    public SignalGeneratorService(
        AppDbContext db,
        BitgetApiClient bitgetClient,
        ILogger<SignalGeneratorService> logger)
    {
        _db = db;
        _bitgetClient = bitgetClient;
        _logger = logger;
    }

    public async Task<List<TradingSignal>> EvaluateSignalsAsync()
    {
        var activeSignals = await _db.TradingSignals
            .Where(s => s.Status == SignalStatus.Active || s.Status == SignalStatus.TP1Hit || s.Status == SignalStatus.TP2Hit)
            .ToListAsync();

        var updatedSignals = new List<TradingSignal>();

        foreach (var signal in activeSignals)
        {
            try
            {
                // Check expiry
                if (signal.ExpiresAt.HasValue && signal.ExpiresAt.Value < DateTime.UtcNow)
                {
                    signal.Status = SignalStatus.Expired;
                    signal.UpdatedAt = DateTime.UtcNow;
                    updatedSignals.Add(signal);
                    continue;
                }

                var ticker = await _bitgetClient.GetSpotTickerAsync(signal.Symbol);
                if (ticker == null || !decimal.TryParse(ticker.LastPr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var currentPrice))
                    continue;

                var previousStatus = signal.Status;
                var changed = false;

                if (signal.Direction == SignalDirection.Long)
                {
                    if (currentPrice <= signal.StopLoss)
                    {
                        signal.Status = SignalStatus.StopLoss;
                        changed = true;
                    }
                    else if (signal.TakeProfit3.HasValue && currentPrice >= signal.TakeProfit3.Value && signal.Status != SignalStatus.TP3Hit)
                    {
                        signal.Status = SignalStatus.TP3Hit;
                        changed = true;
                    }
                    else if (signal.TakeProfit2.HasValue && currentPrice >= signal.TakeProfit2.Value && signal.Status != SignalStatus.TP2Hit && signal.Status != SignalStatus.TP3Hit)
                    {
                        signal.Status = SignalStatus.TP2Hit;
                        changed = true;
                    }
                    else if (currentPrice >= signal.TakeProfit1 && signal.Status == SignalStatus.Active)
                    {
                        signal.Status = SignalStatus.TP1Hit;
                        changed = true;
                    }
                }
                else // Short
                {
                    if (currentPrice >= signal.StopLoss)
                    {
                        signal.Status = SignalStatus.StopLoss;
                        changed = true;
                    }
                    else if (signal.TakeProfit3.HasValue && currentPrice <= signal.TakeProfit3.Value && signal.Status != SignalStatus.TP3Hit)
                    {
                        signal.Status = SignalStatus.TP3Hit;
                        changed = true;
                    }
                    else if (signal.TakeProfit2.HasValue && currentPrice <= signal.TakeProfit2.Value && signal.Status != SignalStatus.TP2Hit && signal.Status != SignalStatus.TP3Hit)
                    {
                        signal.Status = SignalStatus.TP2Hit;
                        changed = true;
                    }
                    else if (currentPrice <= signal.TakeProfit1 && signal.Status == SignalStatus.Active)
                    {
                        signal.Status = SignalStatus.TP1Hit;
                        changed = true;
                    }
                }

                if (changed)
                {
                    signal.UpdatedAt = DateTime.UtcNow;
                    updatedSignals.Add(signal);
                    _logger.LogInformation("Signal {Id} ({Symbol}) status changed from {Old} to {New} at price {Price}",
                        signal.Id, signal.Symbol, previousStatus, signal.Status, currentPrice);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating signal {Id}", signal.Id);
            }
        }

        if (updatedSignals.Count > 0)
            await _db.SaveChangesAsync();

        // Auto signal generation based on RSI/MA for top symbols
        await TryGenerateAutoSignalsAsync();

        return updatedSignals;
    }

    public async Task<TradingSignal?> CreateSignalAsync(CreateSignalDto dto, string createdById)
    {
        try
        {
            var rrRatio = dto.StopLoss > 0
                ? Math.Abs(dto.TakeProfit1 - dto.EntryPrice) / Math.Abs(dto.EntryPrice - dto.StopLoss)
                : 0;

            var signal = new TradingSignal
            {
                Symbol = dto.Symbol.ToUpperInvariant(),
                Direction = dto.Direction,
                EntryPrice = dto.EntryPrice,
                StopLoss = dto.StopLoss,
                TakeProfit1 = dto.TakeProfit1,
                TakeProfit2 = dto.TakeProfit2,
                TakeProfit3 = dto.TakeProfit3,
                RiskRewardRatio = Math.Round(rrRatio, 2),
                Confidence = dto.Confidence,
                Timeframe = dto.Timeframe,
                MinTier = dto.MinTier,
                ExpiresAt = dto.ExpiresAt,
                Source = dto.Source,
                Notes = dto.Notes,
                CreatedById = createdById,
                Status = SignalStatus.Active
            };

            _db.TradingSignals.Add(signal);
            await _db.SaveChangesAsync();
            return signal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating signal");
            return null;
        }
    }

    public async Task<bool> UpdateSignalStatusAsync(Guid signalId, SignalStatus status)
    {
        var signal = await _db.TradingSignals.FindAsync(signalId);
        if (signal == null) return false;

        signal.Status = status;
        signal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task TryGenerateAutoSignalsAsync()
    {
        var symbols = new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT" };

        foreach (var symbol in symbols)
        {
            try
            {
                var candles = await _bitgetClient.GetSpotCandlesAsync(symbol, "1H", 50);
                if (candles == null || candles.Count < 20) continue;

                var closes = candles
                    .Select(c => decimal.TryParse(c.Close, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0)
                    .Where(v => v > 0)
                    .ToList();

                if (closes.Count < 20) continue;

                var rsi = CalculateRsi(closes, 14);
                var ma20 = closes.TakeLast(20).Average();
                var ma50 = closes.Count >= 50 ? closes.TakeLast(50).Average() : closes.Average();
                var lastPrice = closes.Last();

                // Already have an active auto signal for this symbol?
                var hasActiveSignal = await _db.TradingSignals
                    .AnyAsync(s => s.Symbol == symbol && s.Status == SignalStatus.Active && s.Source == SignalSource.Auto);

                if (hasActiveSignal) continue;

                SignalDirection? direction = null;
                int confidence = 0;

                // Long: RSI oversold + MA20 above MA50 (uptrend) + price near MA20
                if (rsi < 35 && ma20 > ma50 && lastPrice > ma20 * 0.99m)
                {
                    direction = SignalDirection.Long;
                    confidence = rsi < 25 ? 75 : 60;
                }
                // Short: RSI overbought + MA20 below MA50 (downtrend)
                else if (rsi > 65 && ma20 < ma50 && lastPrice < ma20 * 1.01m)
                {
                    direction = SignalDirection.Short;
                    confidence = rsi > 75 ? 75 : 60;
                }

                if (direction.HasValue)
                {
                    var atr = CalculateAtr(candles, 14);
                    var stopDistance = atr * 1.5m;
                    var tpDistance = atr * 2.5m;

                    var entryPrice = lastPrice;
                    var stopLoss = direction == SignalDirection.Long ? entryPrice - stopDistance : entryPrice + stopDistance;
                    var tp1 = direction == SignalDirection.Long ? entryPrice + tpDistance : entryPrice - tpDistance;
                    var tp2 = direction == SignalDirection.Long ? entryPrice + tpDistance * 2 : entryPrice - tpDistance * 2;

                    var autoSignal = new TradingSignal
                    {
                        Symbol = symbol,
                        Direction = direction.Value,
                        EntryPrice = Math.Round(entryPrice, 2),
                        StopLoss = Math.Round(stopLoss, 2),
                        TakeProfit1 = Math.Round(tp1, 2),
                        TakeProfit2 = Math.Round(tp2, 2),
                        RiskRewardRatio = Math.Round(tpDistance / stopDistance, 2),
                        Confidence = confidence,
                        Timeframe = "1h",
                        MinTier = SubscriptionTier.Pro,
                        Source = SignalSource.Auto,
                        ExpiresAt = DateTime.UtcNow.AddDays(1),
                        Notes = $"Auto-generated: RSI={rsi:F1}, MA20={ma20:F2}, MA50={ma50:F2}",
                        CreatedById = "system"
                    };

                    _db.TradingSignals.Add(autoSignal);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Auto signal generated for {Symbol}: {Direction}, RSI={Rsi:F1}", symbol, direction, rsi);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating auto signal for {Symbol}", symbol);
            }
        }
    }

    private static decimal CalculateRsi(List<decimal> closes, int period = 14)
    {
        if (closes.Count < period + 1) return 50;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100;
        var rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    private static decimal CalculateAtr(List<CandleData> candles, int period = 14)
    {
        if (candles.Count < 2) return 0;

        var trueRanges = new List<decimal>();
        for (int i = 1; i < candles.Count; i++)
        {
            if (!decimal.TryParse(candles[i].High, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var high)) continue;
            if (!decimal.TryParse(candles[i].Low, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var low)) continue;
            if (!decimal.TryParse(candles[i - 1].Close, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var prevClose)) continue;

            var tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
            trueRanges.Add(tr);
        }

        return trueRanges.Count > 0 ? trueRanges.TakeLast(period).Average() : 0;
    }
}
