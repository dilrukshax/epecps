using System.ComponentModel.DataAnnotations;
using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for creating a new score item
/// </summary>
public class CreateScoreItemDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public ScoreItemType ItemType { get; set; } = ScoreItemType.Rating;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal MaxScore { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? WeightWithinCategory { get; set; }

    public bool IsMandatory { get; set; } = false;

    public bool EvidenceRequired { get; set; } = false;

    [MaxLength(500)]
    public string? EvidenceHint { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; } = 0;
}
