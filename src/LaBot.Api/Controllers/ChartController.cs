using LaBot.Api.Data;
using LaBot.Api.Models.Domain;
using LaBot.Api.Models.Dto;
using LaBot.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaBot.Api.Controllers;

[ApiController]
[Route("api/charts")]
[Authorize]
public class ChartController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IChartAnalysisService _chartService;
    private readonly ILogger<ChartController> _logger;

    public ChartController(AppDbContext db, IChartAnalysisService chartService, ILogger<ChartController> logger)
    {
        _db = db;
        _chartService = chartService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadChart([FromForm] ChartUploadDto dto, IFormFile file)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var imagePath = await _chartService.SaveChartImageAsync(file);

            var chart = new ChartAnalysis
            {
                Title = dto.Title,
                ImagePath = imagePath,
                PlaybookId = dto.PlaybookId,
                CreatedById = userId
            };

            _db.ChartAnalyses.Add(chart);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChart), new { id = chart.Id }, new ChartResponseDto
            {
                Id = chart.Id, Title = chart.Title, ImagePath = chart.ImagePath,
                PlaybookId = chart.PlaybookId, CreatedById = chart.CreatedById,
                CreatedAt = chart.CreatedAt, UpdatedAt = chart.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/annotate")]
    public async Task<IActionResult> SaveAnnotations(Guid id, [FromBody] ChartAnnotationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var chart = await _chartService.GetChartAsync(id, userId, isAdmin);
        if (chart == null) return NotFound();

        try
        {
            var annotatedPath = await _chartService.ProcessAnnotationsAsync(chart.ImagePath, dto.AnnotationData);
            chart.AnnotationData = dto.AnnotationData;
            chart.ImagePath = annotatedPath;
            chart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new ChartResponseDto
            {
                Id = chart.Id, Title = chart.Title, ImagePath = chart.ImagePath,
                AnnotationData = chart.AnnotationData, PlaybookId = chart.PlaybookId,
                CreatedById = chart.CreatedById, CreatedAt = chart.CreatedAt, UpdatedAt = chart.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing annotations for chart {Id}", id);
            return StatusCode(500, new { error = "Failed to process annotations." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCharts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var charts = await _chartService.GetUserChartsAsync(userId);

        return Ok(charts.Select(c => new ChartResponseDto
        {
            Id = c.Id, Title = c.Title, ImagePath = c.ImagePath,
            AnnotationData = c.AnnotationData, PlaybookId = c.PlaybookId,
            PlaybookName = c.Playbook?.Name, CreatedById = c.CreatedById,
            CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetChart(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var chart = await _chartService.GetChartAsync(id, userId, isAdmin);
        if (chart == null) return NotFound();

        return Ok(new ChartResponseDto
        {
            Id = chart.Id, Title = chart.Title, ImagePath = chart.ImagePath,
            AnnotationData = chart.AnnotationData, PlaybookId = chart.PlaybookId,
            PlaybookName = chart.Playbook?.Name, CreatedById = chart.CreatedById,
            CreatedAt = chart.CreatedAt, UpdatedAt = chart.UpdatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteChart(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var chart = await _chartService.GetChartAsync(id, userId, isAdmin);
        if (chart == null) return NotFound();

        await _chartService.DeleteChartImageAsync(chart.ImagePath);
        _db.ChartAnalyses.Remove(chart);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
