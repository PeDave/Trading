using LaBot.Api.Data;
using LaBot.Api.Hubs;
using LaBot.Api.Models.Domain;
using LaBot.Api.Models.Dto;
using LaBot.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaBot.Api.Controllers;

[ApiController]
[Route("api/signals")]
[Authorize]
public class SignalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISignalGeneratorService _signalService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IHubContext<SignalHub> _hubContext;
    private readonly ILogger<SignalController> _logger;

    public SignalController(
        AppDbContext db,
        ISignalGeneratorService signalService,
        ISubscriptionService subscriptionService,
        IHubContext<SignalHub> hubContext,
        ILogger<SignalController> logger)
    {
        _db = db;
        _signalService = signalService;
        _subscriptionService = subscriptionService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSignals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? symbol = null,
        [FromQuery] string? status = null,
        [FromQuery] string? direction = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        SubscriptionTier userTier;
        if (isAdmin)
            userTier = SubscriptionTier.ProPlus;
        else
            userTier = await _subscriptionService.GetUserTierAsync(userId);

        var query = _db.TradingSignals.AsQueryable();

        if (!isAdmin)
            query = query.Where(s => s.MinTier <= userTier);

        if (!string.IsNullOrWhiteSpace(symbol))
            query = query.Where(s => s.Symbol == symbol.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SignalStatus>(status, true, out var statusEnum))
            query = query.Where(s => s.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(direction) && Enum.TryParse<SignalDirection>(direction, true, out var dirEnum))
            query = query.Where(s => s.Direction == dirEnum);

        var total = await query.CountAsync();
        var signals = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SignalResponseDto
            {
                Id = s.Id,
                Symbol = s.Symbol,
                Direction = s.Direction.ToString(),
                EntryPrice = s.EntryPrice,
                StopLoss = s.StopLoss,
                TakeProfit1 = s.TakeProfit1,
                TakeProfit2 = s.TakeProfit2,
                TakeProfit3 = s.TakeProfit3,
                RiskRewardRatio = s.RiskRewardRatio,
                Confidence = s.Confidence,
                Timeframe = s.Timeframe,
                Status = s.Status.ToString(),
                MinTier = s.MinTier.ToString(),
                Source = s.Source.ToString(),
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                ExpiresAt = s.ExpiresAt,
                CreatedById = s.CreatedById
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, signals });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSignal(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        var userTier = isAdmin ? SubscriptionTier.ProPlus : await _subscriptionService.GetUserTierAsync(userId);

        var signal = await _db.TradingSignals.FindAsync(id);
        if (signal == null) return NotFound();
        if (!isAdmin && signal.MinTier > userTier) return StatusCode(403, new { error = "Insufficient subscription tier." });

        return Ok(new SignalResponseDto
        {
            Id = signal.Id, Symbol = signal.Symbol, Direction = signal.Direction.ToString(),
            EntryPrice = signal.EntryPrice, StopLoss = signal.StopLoss, TakeProfit1 = signal.TakeProfit1,
            TakeProfit2 = signal.TakeProfit2, TakeProfit3 = signal.TakeProfit3,
            RiskRewardRatio = signal.RiskRewardRatio, Confidence = signal.Confidence,
            Timeframe = signal.Timeframe, Status = signal.Status.ToString(),
            MinTier = signal.MinTier.ToString(), Source = signal.Source.ToString(),
            Notes = signal.Notes, CreatedAt = signal.CreatedAt, UpdatedAt = signal.UpdatedAt,
            ExpiresAt = signal.ExpiresAt, CreatedById = signal.CreatedById
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSignal([FromBody] CreateSignalDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var signal = await _signalService.CreateSignalAsync(dto, userId);
        if (signal == null) return StatusCode(500, new { error = "Failed to create signal." });

        // Broadcast to tier group
        var groupName = SignalHub.GetTierGroupName(signal.MinTier);
        await _hubContext.Clients.Group(groupName).SendAsync("NewSignal", new SignalResponseDto
        {
            Id = signal.Id, Symbol = signal.Symbol, Direction = signal.Direction.ToString(),
            EntryPrice = signal.EntryPrice, StopLoss = signal.StopLoss, TakeProfit1 = signal.TakeProfit1,
            TakeProfit2 = signal.TakeProfit2, TakeProfit3 = signal.TakeProfit3,
            RiskRewardRatio = signal.RiskRewardRatio, Confidence = signal.Confidence,
            Timeframe = signal.Timeframe, Status = signal.Status.ToString(),
            MinTier = signal.MinTier.ToString(), Source = signal.Source.ToString(),
            CreatedAt = signal.CreatedAt, UpdatedAt = signal.UpdatedAt, ExpiresAt = signal.ExpiresAt,
            CreatedById = signal.CreatedById
        });

        return CreatedAtAction(nameof(GetSignal), new { id = signal.Id }, signal);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSignal(Guid id, [FromBody] UpdateSignalDto dto)
    {
        var signal = await _db.TradingSignals.FindAsync(id);
        if (signal == null) return NotFound();

        if (dto.Status.HasValue) signal.Status = dto.Status.Value;
        if (dto.TakeProfit1.HasValue) signal.TakeProfit1 = dto.TakeProfit1.Value;
        if (dto.TakeProfit2.HasValue) signal.TakeProfit2 = dto.TakeProfit2.Value;
        if (dto.TakeProfit3.HasValue) signal.TakeProfit3 = dto.TakeProfit3.Value;
        if (dto.StopLoss.HasValue) signal.StopLoss = dto.StopLoss.Value;
        if (dto.Confidence.HasValue) signal.Confidence = dto.Confidence.Value;
        if (dto.ExpiresAt.HasValue) signal.ExpiresAt = dto.ExpiresAt.Value;
        if (dto.Notes != null) signal.Notes = dto.Notes;

        signal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(signal);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSignal(Guid id)
    {
        var signal = await _db.TradingSignals.FindAsync(id);
        if (signal == null) return NotFound();

        _db.TradingSignals.Remove(signal);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetSignalPerformance()
    {
        var total = await _db.TradingSignals.CountAsync();
        var byStatus = await _db.TradingSignals
            .GroupBy(s => s.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        var bySymbol = await _db.TradingSignals
            .GroupBy(s => s.Symbol)
            .Select(g => new
            {
                symbol = g.Key,
                total = g.Count(),
                wins = g.Count(s => s.Status == SignalStatus.TP1Hit || s.Status == SignalStatus.TP2Hit || s.Status == SignalStatus.TP3Hit),
                losses = g.Count(s => s.Status == SignalStatus.StopLoss)
            })
            .OrderByDescending(g => g.total)
            .Take(10)
            .ToListAsync();

        var winCount = byStatus.Where(s => s.status is "TP1Hit" or "TP2Hit" or "TP3Hit").Sum(s => s.count);
        var lossCount = byStatus.FirstOrDefault(s => s.status == "StopLoss")?.count ?? 0;
        var winRate = total > 0 ? (double)(winCount) / (winCount + lossCount) * 100 : 0;

        return Ok(new
        {
            total,
            winRate = Math.Round(winRate, 2),
            byStatus,
            topSymbols = bySymbol
        });
    }
}
