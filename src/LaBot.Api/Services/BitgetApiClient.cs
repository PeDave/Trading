using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LaBot.Api.Models.Bitget;

namespace LaBot.Api.Services;

public class BitgetApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<BitgetApiClient> _logger;
    private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(20, 20);
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public BitgetApiClient(HttpClient httpClient, IConfiguration config, ILogger<BitgetApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        var baseUrl = _config["Bitget:BaseUrl"] ?? "https://api.bitget.com";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private string GenerateSignature(string timestamp, string method, string requestPath, string body)
    {
        var message = timestamp + method + requestPath + body;
        var keyBytes = Encoding.UTF8.GetBytes(_config["Bitget:ApiSecret"] ?? "");
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hash);
    }

    private void AddAuthHeaders(HttpRequestMessage request, string method, string requestPath, string body = "")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signature = GenerateSignature(timestamp, method, requestPath, body);

        request.Headers.Add("ACCESS-KEY", _config["Bitget:ApiKey"] ?? "");
        request.Headers.Add("ACCESS-SIGN", signature);
        request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
        request.Headers.Add("ACCESS-PASSPHRASE", _config["Bitget:ApiPassphrase"] ?? "");
        request.Headers.Add("locale", "en-US");
    }

    private async Task<T?> SendWithRetryAsync<T>(HttpRequestMessage request, int maxRetries = 3)
    {
        await _rateLimiter.WaitAsync();
        try
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<BitgetResponse<T>>(content, _jsonOptions);
                        if (result?.IsSuccess == true)
                            return result.Data;

                        _logger.LogWarning("Bitget API error: {Code} - {Msg}", result?.Code, result?.Msg);
                        return default;
                    }

                    if ((int)response.StatusCode == 429 && attempt < maxRetries)
                    {
                        var delay = (int)Math.Pow(2, attempt) * 1000;
                        _logger.LogWarning("Rate limited by Bitget, retrying in {Delay}ms", delay);
                        await Task.Delay(delay);
                        request = CloneRequest(request);
                        continue;
                    }

                    _logger.LogError("Bitget HTTP error: {StatusCode} - {Content}", response.StatusCode, content);
                    return default;
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    var delay = (int)Math.Pow(2, attempt) * 1000;
                    _logger.LogWarning(ex, "HTTP request failed, retrying in {Delay}ms (attempt {Attempt})", delay, attempt + 1);
                    await Task.Delay(delay);
                    request = CloneRequest(request);
                }
            }
            return default;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (original.Content != null)
        {
            var contentBytes = original.Content.ReadAsByteArrayAsync().Result;
            clone.Content = new ByteArrayContent(contentBytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }

    // ===== Spot Market Data (Public) =====

    public async Task<List<SpotTickerData>?> GetSpotTickersAsync()
    {
        var path = "/api/v2/spot/market/tickers";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendWithRetryAsync<List<SpotTickerData>>(request);
    }

    public async Task<SpotTickerData?> GetSpotTickerAsync(string symbol)
    {
        var path = $"/api/v2/spot/market/tickers?symbol={Uri.EscapeDataString(symbol)}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        var result = await SendWithRetryAsync<List<SpotTickerData>>(request);
        return result?.FirstOrDefault();
    }

    public async Task<List<string[]>?> GetSpotCandlesRawAsync(string symbol, string granularity = "1H", int limit = 100)
    {
        var path = $"/api/v2/spot/market/candles?symbol={Uri.EscapeDataString(symbol)}&granularity={granularity}&limit={limit}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendWithRetryAsync<List<string[]>>(request);
    }

    public async Task<List<CandleData>?> GetSpotCandlesAsync(string symbol, string granularity = "1H", int limit = 100)
    {
        var raw = await GetSpotCandlesRawAsync(symbol, granularity, limit);
        return raw?.Select(arr => CandleData.FromArray(arr)).ToList();
    }

    public async Task<object?> GetSpotOrderBookAsync(string symbol, int limit = 20)
    {
        var path = $"/api/v2/spot/market/orderbook?symbol={Uri.EscapeDataString(symbol)}&limit={limit}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendWithRetryAsync<object>(request);
    }

    public async Task<object?> GetSpotFillsAsync(string symbol, int limit = 20)
    {
        var path = $"/api/v2/spot/market/fills?symbol={Uri.EscapeDataString(symbol)}&limit={limit}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendWithRetryAsync<object>(request);
    }

    // ===== Spot Trading (Private) =====

    public async Task<OrderResponseData?> PlaceSpotOrderAsync(PlaceSpotOrderRequest orderRequest)
    {
        var path = "/api/v2/spot/trade/place-order";
        var body = JsonSerializer.Serialize(orderRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, "POST", path, body);
        return await SendWithRetryAsync<OrderResponseData>(request);
    }

    public async Task<OrderResponseData?> CancelSpotOrderAsync(CancelOrderRequest cancelRequest)
    {
        var path = "/api/v2/spot/trade/cancel-order";
        var body = JsonSerializer.Serialize(cancelRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, "POST", path, body);
        return await SendWithRetryAsync<OrderResponseData>(request);
    }

    public async Task<object?> BatchSpotOrdersAsync(object batchRequest)
    {
        var path = "/api/v2/spot/trade/batch-orders";
        var body = JsonSerializer.Serialize(batchRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, "POST", path, body);
        return await SendWithRetryAsync<object>(request);
    }

    public async Task<OrderDetailData?> GetSpotOrderInfoAsync(string symbol, string orderId)
    {
        var path = $"/api/v2/spot/trade/orderInfo?symbol={Uri.EscapeDataString(symbol)}&orderId={Uri.EscapeDataString(orderId)}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthHeaders(request, "GET", path);
        return await SendWithRetryAsync<OrderDetailData>(request);
    }

    // ===== Spot Account (Private) =====

    public async Task<List<SpotAccountAsset>?> GetSpotAssetsAsync()
    {
        var path = "/api/v2/spot/account/assets";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthHeaders(request, "GET", path);
        return await SendWithRetryAsync<List<SpotAccountAsset>>(request);
    }

    public async Task<object?> GetSpotAccountInfoAsync()
    {
        var path = "/api/v2/spot/account/info";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthHeaders(request, "GET", path);
        return await SendWithRetryAsync<object>(request);
    }

    // ===== Futures Market Data (Public) =====

    public async Task<List<SpotTickerData>?> GetFuturesTickersAsync(string productType = "USDT-FUTURES")
    {
        var path = $"/api/v2/mix/market/tickers?productType={productType}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendWithRetryAsync<List<SpotTickerData>>(request);
    }

    public async Task<List<CandleData>?> GetFuturesCandlesAsync(string symbol, string granularity = "1H", int limit = 100, string productType = "USDT-FUTURES")
    {
        var path = $"/api/v2/mix/market/candles?symbol={Uri.EscapeDataString(symbol)}&productType={productType}&granularity={granularity}&limit={limit}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        var raw = await SendWithRetryAsync<List<string[]>>(request);
        return raw?.Select(arr => CandleData.FromArray(arr)).ToList();
    }

    public async Task<object?> GetFuturesDepthAsync(string symbol, int limit = 20, string productType = "USDT-FUTURES")
    {
        var path = $"/api/v2/mix/market/merge-depth?symbol={Uri.EscapeDataString(symbol)}&productType={productType}&limit={limit}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendWithRetryAsync<object>(request);
    }

    // ===== Futures Trading (Private) =====

    public async Task<OrderResponseData?> PlaceFuturesOrderAsync(PlaceFuturesOrderRequest orderRequest)
    {
        var path = "/api/v2/mix/order/place-order";
        var body = JsonSerializer.Serialize(orderRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, "POST", path, body);
        return await SendWithRetryAsync<OrderResponseData>(request);
    }

    public async Task<OrderResponseData?> CancelFuturesOrderAsync(CancelOrderRequest cancelRequest)
    {
        var path = "/api/v2/mix/order/cancel-order";
        var body = JsonSerializer.Serialize(cancelRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, "POST", path, body);
        return await SendWithRetryAsync<OrderResponseData>(request);
    }

    public async Task<List<FuturesPositionData>?> GetAllPositionsAsync(string productType = "USDT-FUTURES", string marginCoin = "USDT")
    {
        var path = $"/api/v2/mix/position/all-position?productType={productType}&marginCoin={marginCoin}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthHeaders(request, "GET", path);
        return await SendWithRetryAsync<List<FuturesPositionData>>(request);
    }

    public async Task<FuturesAccountData?> GetFuturesAccountAsync(string productType = "USDT-FUTURES", string marginCoin = "USDT")
    {
        var path = $"/api/v2/mix/account/account?productType={productType}&marginCoin={marginCoin}";
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthHeaders(request, "GET", path);
        return await SendWithRetryAsync<FuturesAccountData>(request);
    }
}
