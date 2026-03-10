using LaBot.Api.Data;
using LaBot.Api.Models.Domain;
using LaBot.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaBot.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ISubscriptionService subscriptionService,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _db = db;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email!.Contains(search) || u.UserName!.Contains(search));

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id, u.UserName, u.Email, u.IsActive,
                tier = u.SubscriptionTier.ToString(),
                u.CreatedAt, u.LastLogin
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, users });
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var orders = await _db.Orders.CountAsync(o => o.UserId == id);
        var subscriptions = await _db.Subscriptions.Where(s => s.UserId == id).ToListAsync();

        return Ok(new
        {
            user.Id, user.UserName, user.Email, user.IsActive,
            tier = user.SubscriptionTier.ToString(),
            roles, user.CreatedAt, user.LastLogin,
            orderCount = orders,
            subscriptions = subscriptions.Select(s => new
            {
                s.Id, tier = s.Tier.ToString(), s.StartDate, s.EndDate, s.IsActive
            })
        });
    }

    [HttpPut("users/{id}/tier")]
    public async Task<IActionResult> UpdateUserTier(string id, [FromBody] UpdateTierDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var success = await _subscriptionService.UpdateUserTierAsync(id, dto.Tier);
        if (!success) return NotFound(new { error = "User not found." });

        _logger.LogInformation("Admin {AdminId} updated tier for user {UserId} to {Tier}", adminId, id, dto.Tier);
        return Ok(new { message = "Tier updated successfully.", tier = dto.Tier.ToString() });
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateStatusDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == adminId)
            return BadRequest(new { error = "Cannot modify your own account status." });

        user.IsActive = dto.IsActive;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Admin {AdminId} {Action} user {UserId}", adminId, dto.IsActive ? "enabled" : "disabled", id);
        return Ok(new { message = $"User {(dto.IsActive ? "enabled" : "disabled")} successfully." });
    }

    [HttpGet("feature-gates")]
    public async Task<IActionResult> GetFeatureGates()
    {
        var gates = await _subscriptionService.GetFeatureGatesAsync();
        return Ok(gates);
    }

    [HttpPost("feature-gates")]
    public async Task<IActionResult> UpsertFeatureGate([FromBody] FeatureGate gate)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        gate.UpdatedBy = adminId;
        var result = await _subscriptionService.UpsertFeatureGateAsync(gate);
        return Ok(result);
    }

    [HttpDelete("feature-gates/{id:guid}")]
    public async Task<IActionResult> DeleteFeatureGate(Guid id)
    {
        var success = await _subscriptionService.DeleteFeatureGateAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("activity-logs")]
    public async Task<IActionResult> GetActivityLogs([FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);
        var recentSignals = await _db.TradingSignals
            .Include(s => s.CreatedBy)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit / 2)
            .Select(s => new
            {
                type = "signal_created",
                userId = s.CreatedById,
                userName = s.CreatedBy != null ? s.CreatedBy.UserName : "system",
                description = $"Signal created: {s.Symbol} {s.Direction} ({s.Source})",
                timestamp = s.CreatedAt
            })
            .ToListAsync();

        var recentOrders = await _db.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit / 2)
            .Select(o => new
            {
                type = "order_placed",
                userId = o.UserId,
                userName = o.User != null ? o.User.UserName : "unknown",
                description = $"Order placed: {o.Symbol} {o.Side} {o.Quantity}",
                timestamp = o.CreatedAt
            })
            .ToListAsync();

        var combined = recentSignals.Cast<object>()
            .Concat(recentOrders)
            .OrderByDescending(x => ((dynamic)x).timestamp)
            .Take(limit)
            .ToList();

        return Ok(combined);
    }
}

public class UpdateTierDto
{
    public SubscriptionTier Tier { get; set; }
}

public class UpdateStatusDto
{
    public bool IsActive { get; set; }
}
