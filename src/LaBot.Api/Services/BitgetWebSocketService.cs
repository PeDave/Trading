using LaBot.Api.Data;
using LaBot.Api.Hubs;
using LaBot.Api.Models.Bitget;
using Microsoft.AspNetCore.SignalR;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LaBot.Api.Services;

public class BitgetWebSocketService : IHostedService, IDisposable
{
    private readonly ILogger<BitgetWebSocketService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<SignalHub> _hubContext;
    private readonly IConfiguration _config;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private bool _disposed;

    private static readonly string[] WatchedSymbols = new[]
    {
        "BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT", "XRPUSDT",
        "ADAUSDT", "AVAXUSDT", "DOTUSDT", "LINKUSDT", "MATICUSDT"
    };

    public BitgetWebSocketService(
        ILogger<BitgetWebSocketService> logger,
        IServiceProvider serviceProvider,
        IHubContext<SignalHub> hubContext,
        IConfiguration config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BitgetWebSocketService starting...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = Task.Run(() => RunWebSocketLoopAsync(_cts.Token), _cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BitgetWebSocketService stopping...");
        _cts?.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping", cancellationToken);
        }
        if (_receiveTask != null)
            await Task.WhenAny(_receiveTask, Task.Delay(5000, cancellationToken));
    }

    private async Task RunWebSocketLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await ConnectAndSubscribeAsync(token);
                await ReceiveLoopAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket error, reconnecting in 5 seconds...");
                await Task.Delay(5000, token);
            }
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken token)
    {
        _ws?.Dispose();
        _ws = new ClientWebSocket();
        var wsUrl = "wss://ws.bitget.com/v2/ws/public";
        _logger.LogInformation("Connecting to Bitget WebSocket: {Url}", wsUrl);
        await _ws.ConnectAsync(new Uri(wsUrl), token);
        _logger.LogInformation("Connected to Bitget WebSocket");

        var args = WatchedSymbols.Select(s => new
        {
            instType = "SPOT",
            channel = "ticker",
            instId = s
        }).ToList();

        var subscribeMsg = JsonSerializer.Serialize(new { op = "subscribe", args });
        var bytes = Encoding.UTF8.GetBytes(subscribeMsg);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        _logger.LogInformation("Subscribed to {Count} ticker channels", WatchedSymbols.Length);
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        while (!token.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("WebSocket closed by server");
                break;
            }

            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                var message = messageBuilder.ToString();
                messageBuilder.Clear();
                await ProcessMessageAsync(message);
            }
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            if (message == "pong" || message.Contains("\"event\":\"pong\""))
                return;

            if (message.Contains("\"event\":\"subscribe\""))
            {
                _logger.LogDebug("Subscription confirmed: {Message}", message);
                return;
            }

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataElement) &&
                root.TryGetProperty("arg", out var argElement))
            {
                var channel = argElement.TryGetProperty("channel", out var ch) ? ch.GetString() : null;
                var instId = argElement.TryGetProperty("instId", out var id) ? id.GetString() : null;

                if (channel == "ticker" && instId != null)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var ticker = new
                        {
                            symbol = instId,
                            lastPrice = item.TryGetProperty("lastPr", out var lp) ? lp.GetString() : "0",
                            priceChangePercent = item.TryGetProperty("priceChangePercent", out var pcp) ? pcp.GetString() : "0",
                            high24h = item.TryGetProperty("high24h", out var h) ? h.GetString() : "0",
                            low24h = item.TryGetProperty("low24h", out var l) ? l.GetString() : "0",
                            quoteVolume = item.TryGetProperty("quoteVolume", out var qv) ? qv.GetString() : "0",
                            ts = item.TryGetProperty("ts", out var ts) ? ts.GetString() : ""
                        };
                        await _hubContext.Clients.All.SendAsync("TickerUpdate", ticker);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse WebSocket message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _ws?.Dispose();
            _disposed = true;
        }
    }
}
