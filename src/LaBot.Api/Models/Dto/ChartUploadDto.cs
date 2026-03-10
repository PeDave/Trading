using System.ComponentModel.DataAnnotations;

namespace LaBot.Api.Models.Dto;

public class ChartUploadDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    public Guid? PlaybookId { get; set; }
}

public class ChartAnnotationDto
{
    [Required]
    public string AnnotationData { get; set; } = "";
}

public class ChartResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string? AnnotationData { get; set; }
    public Guid? PlaybookId { get; set; }
    public string? PlaybookName { get; set; }
    public string CreatedById { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
