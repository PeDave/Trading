using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Domain;

public class ChartAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required, MaxLength(500)]
    public string ImagePath { get; set; } = "";

    public string? AnnotationData { get; set; }

    public Guid? PlaybookId { get; set; }
    public Playbook? Playbook { get; set; }

    [Required]
    public string CreatedById { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
