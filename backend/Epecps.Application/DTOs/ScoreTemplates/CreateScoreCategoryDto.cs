using System.ComponentModel.DataAnnotations;

namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for creating a new score category
/// </summary>
public class CreateScoreCategoryDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal WeightPercent { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxScore { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; } = 0;
}
