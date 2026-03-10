using LaBot.Api.Data;
using LaBot.Api.Models.Bitget;
using LaBot.Api.Models.Domain;
using LaBot.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaBot.Api.Controllers;

[ApiController]
[Route("api/trading")]
[Authorize]
public class TradingController : ControllerBase
{
    private readonly BitgetApiClient _bitgetClient;
    private readonly AppDbContext _db;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<TradingController> _logger;

    public TradingController(
        BitgetApiClient bitgetClient,
        AppDbContext db,
        ISubscriptionService subscriptionService,
        ILogger<TradingController> logger)
    {
        _bitgetClient = bitgetClient;
        _db = db;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    private async Task<bool> RequireProPlusOrAdmin()
    {
        if (User.IsInRole("Admin")) return true;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var tier = await _subscriptionService.GetUserTierAsync(userId);
        return tier >= SubscriptionTier.ProPlus;
    }

    // ===== Spot Trading =====

    [HttpPost("spot/order")]
    public async Task<IActionResult> PlaceSpotOrder([FromBody] PlaceSpotOrderRequest request)
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required for trading." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bitgetClient.PlaceSpotOrderAsync(request);
        if (result == null) return StatusCode(502, new { error = "Failed to place order on Bitget." });

        // Record order in DB
        var order = new Order
        {
            UserId = userId,
            BitgetOrderId = result.OrderId,
            Symbol = request.Symbol,
            Side = request.Side.Equals("buy", StringComparison.OrdinalIgnoreCase) ? OrderSide.Buy : OrderSide.Sell,
            OrderType = ParseOrderType(request.OrderType),
            Price = request.Price != null && decimal.TryParse(request.Price, out var p) ? p : null,
            Quantity = decimal.TryParse(request.Size, out var q) ? q : 0,
            Status = "pending"
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new { orderId = result.OrderId, internalId = order.Id });
    }

    [HttpPost("spot/cancel")]
    public async Task<IActionResult> CancelSpotOrder([FromBody] CancelOrderRequest request)
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required for trading." });

        var result = await _bitgetClient.CancelSpotOrderAsync(request);
        if (result == null) return StatusCode(502, new { error = "Failed to cancel order on Bitget." });

        // Update local order status
        if (!string.IsNullOrEmpty(result.OrderId))
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.BitgetOrderId == result.OrderId);
            if (order != null)
            {
                order.Status = "cancelled";
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        return Ok(result);
    }

    [HttpGet("spot/order/{orderId}")]
    public async Task<IActionResult> GetSpotOrder(string orderId, [FromQuery] string symbol = "")
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required." });

        var result = await _bitgetClient.GetSpotOrderInfoAsync(symbol, orderId);
        if (result == null) return NotFound(new { error = "Order not found." });
        return Ok(result);
    }

    [HttpGet("account/spot")]
    public async Task<IActionResult> GetSpotAccount()
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required." });

        var result = await _bitgetClient.GetSpotAccountInfoAsync();
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve spot account info." });
        return Ok(result);
    }

    [HttpGet("account/assets")]
    public async Task<IActionResult> GetSpotAssets()
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required." });

        var result = await _bitgetClient.GetSpotAssetsAsync();
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve assets." });
        return Ok(result);
    }

    // ===== Futures Trading =====

    [HttpPost("futures/order")]
    public async Task<IActionResult> PlaceFuturesOrder([FromBody] PlaceFuturesOrderRequest request)
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required for futures trading." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _bitgetClient.PlaceFuturesOrderAsync(request);
        if (result == null) return StatusCode(502, new { error = "Failed to place futures order on Bitget." });

        var order = new Order
        {
            UserId = userId,
            BitgetOrderId = result.OrderId,
            Symbol = request.Symbol,
            Side = request.Side.Contains("buy", StringComparison.OrdinalIgnoreCase) ? OrderSide.Buy : OrderSide.Sell,
            OrderType = ParseOrderType(request.OrderType),
            Price = request.Price != null && decimal.TryParse(request.Price, out var p) ? p : null,
            Quantity = decimal.TryParse(request.Size, out var q) ? q : 0,
            Status = "pending"
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new { orderId = result.OrderId, internalId = order.Id });
    }

    [HttpPost("futures/cancel")]
    public async Task<IActionResult> CancelFuturesOrder([FromBody] CancelOrderRequest request)
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required." });

        var result = await _bitgetClient.CancelFuturesOrderAsync(request);
        if (result == null) return StatusCode(502, new { error = "Failed to cancel futures order." });
        return Ok(result);
    }

    [HttpGet("futures/positions")]
    public async Task<IActionResult> GetFuturesPositions([FromQuery] string productType = "USDT-FUTURES", [FromQuery] string marginCoin = "USDT")
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required." });

        var result = await _bitgetClient.GetAllPositionsAsync(productType, marginCoin);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve positions." });
        return Ok(result);
    }

    [HttpGet("futures/account")]
    public async Task<IActionResult> GetFuturesAccount([FromQuery] string productType = "USDT-FUTURES", [FromQuery] string marginCoin = "USDT")
    {
        if (!await RequireProPlusOrAdmin())
            return StatusCode(403, new { error = "ProPlus subscription required." });

        var result = await _bitgetClient.GetFuturesAccountAsync(productType, marginCoin);
        if (result == null) return StatusCode(502, new { error = "Failed to retrieve futures account." });
        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrderHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Admins can see all orders; regular users only see their own
        var query = User.IsInRole("Admin")
            ? _db.Orders.AsQueryable()
            : _db.Orders.Where(o => o.UserId == userId);

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id, o.BitgetOrderId, o.Symbol,
                side = o.Side.ToString(),
                orderType = o.OrderType.ToString(),
                o.Price, o.Quantity, o.Status, o.FilledQuantity,
                o.CreatedAt, o.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, orders });
    }

    private static OrderType ParseOrderType(string type) => type.ToLowerInvariant() switch
    {
        "limit" => OrderType.Limit,
        "stop_limit" or "stoplimit" => OrderType.StopLimit,
        _ => OrderType.Market
    };
}
