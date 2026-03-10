using LaBot.Api.Data;
using LaBot.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;
using IOPath = System.IO.Path;

namespace LaBot.Api.Services;

public interface IChartAnalysisService
{
    Task<string> SaveChartImageAsync(IFormFile file);
    Task<string> ProcessAnnotationsAsync(string imagePath, string annotationJson);
    Task<bool> DeleteChartImageAsync(string imagePath);
    Task<ChartAnalysis?> GetChartAsync(Guid id, string userId, bool isAdmin);
    Task<List<ChartAnalysis>> GetUserChartsAsync(string userId);
}

public class ChartAnalysisService : IChartAnalysisService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ChartAnalysisService> _logger;

    private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ChartAnalysisService(AppDbContext db, IWebHostEnvironment env, ILogger<ChartAnalysisService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveChartImageAsync(IFormFile file)
    {
        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 10MB limit.");

        var ext = IOPath.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var uploadsDir = IOPath.Combine(_env.WebRootPath, "uploads", "charts");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = IOPath.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/charts/{fileName}";
    }

    public async Task<string> ProcessAnnotationsAsync(string imagePath, string annotationJson)
    {
        try
        {
            var physicalPath = IOPath.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("Chart image not found.", physicalPath);

            var annotations = JsonSerializer.Deserialize<List<AnnotationItem>>(annotationJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AnnotationItem>();

            using var image = await Image.LoadAsync<Rgba32>(physicalPath);

            image.Mutate(ctx =>
            {
                foreach (var annotation in annotations)
                {
                    if (annotation.Type == "line" && annotation.Points?.Count >= 2)
                    {
                        var color = ParseColor(annotation.Color ?? "#FF0000");
                        var pen = Pens.Solid(color, annotation.StrokeWidth > 0 ? annotation.StrokeWidth : 2);
                        for (int i = 0; i < annotation.Points.Count - 1; i++)
                        {
                            var p1 = new PointF(annotation.Points[i].X, annotation.Points[i].Y);
                            var p2 = new PointF(annotation.Points[i + 1].X, annotation.Points[i + 1].Y);
                            ctx.DrawLine(pen, p1, p2);
                        }
                    }
                    else if (annotation.Type == "rect" && annotation.X.HasValue && annotation.Y.HasValue
                             && annotation.Width.HasValue && annotation.Height.HasValue)
                    {
                        var color = ParseColor(annotation.Color ?? "#FF0000");
                        var pen = Pens.Solid(color, annotation.StrokeWidth > 0 ? annotation.StrokeWidth : 2);
                        var rect = new RectangleF(annotation.X.Value, annotation.Y.Value, annotation.Width.Value, annotation.Height.Value);
                        ctx.Draw(pen, rect);
                    }
                    else if (annotation.Type == "circle" && annotation.X.HasValue && annotation.Y.HasValue
                             && annotation.Width.HasValue && annotation.Height.HasValue)
                    {
                        var color = ParseColor(annotation.Color ?? "#FF0000");
                        var pen = Pens.Solid(color, annotation.StrokeWidth > 0 ? annotation.StrokeWidth : 2);
                        var ellipse = new EllipsePolygon(
                            annotation.X.Value + annotation.Width.Value / 2,
                            annotation.Y.Value + annotation.Height.Value / 2,
                            annotation.Width.Value / 2,
                            annotation.Height.Value / 2);
                        ctx.Draw(pen, ellipse);
                    }
                }
            });

            var dir = IOPath.GetDirectoryName(physicalPath)!;
            var name = IOPath.GetFileNameWithoutExtension(physicalPath);
            var annotatedFileName = $"{name}_annotated_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            var annotatedPath = IOPath.Combine(dir, annotatedFileName);
            await image.SaveAsPngAsync(annotatedPath);

            return $"/uploads/charts/{annotatedFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing annotations for image {Path}", imagePath);
            throw;
        }
    }

    public async Task<bool> DeleteChartImageAsync(string imagePath)
    {
        try
        {
            var physicalPath = IOPath.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chart image {Path}", imagePath);
            return false;
        }
    }

    public async Task<ChartAnalysis?> GetChartAsync(Guid id, string userId, bool isAdmin)
    {
        var query = _db.ChartAnalyses
            .Include(c => c.Playbook)
            .Where(c => c.Id == id);

        if (!isAdmin)
            query = query.Where(c => c.CreatedById == userId);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<ChartAnalysis>> GetUserChartsAsync(string userId)
    {
        return await _db.ChartAnalyses
            .Include(c => c.Playbook)
            .Where(c => c.CreatedById == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            return Color.ParseHex(hex.TrimStart('#'));
        }
        catch
        {
            return Color.Red;
        }
    }

    private class AnnotationItem
    {
        public string Type { get; set; } = "";
        public string? Color { get; set; }
        public float StrokeWidth { get; set; } = 2;
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Width { get; set; }
        public float? Height { get; set; }
        public List<AnnotationPoint>? Points { get; set; }
        public string? Text { get; set; }
    }

    private class AnnotationPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
    }
}
