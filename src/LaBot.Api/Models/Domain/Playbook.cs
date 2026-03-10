using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Domain;

public enum PlaybookStatus
{
    Draft,
    Active,
    Archived
}

public class Playbook
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(20)]
    public string Symbol { get; set; } = "";

    [MaxLength(10)]
    public string Timeframe { get; set; } = "1h";

    public string? Scenarios { get; set; }

    public decimal Probability { get; set; }

    public PlaybookStatus Status { get; set; } = PlaybookStatus.Draft;

    [Required]
    public string CreatedById { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChartAnalysis> ChartAnalyses { get; set; } = new List<ChartAnalysis>();
}
