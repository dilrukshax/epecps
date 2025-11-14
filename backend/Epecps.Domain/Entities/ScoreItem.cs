using Epecps.Domain.Enums;

namespace Epecps.Domain.Entities;

/// <summary>
/// Represents an individual scoring item within a category (e.g., specific skill or competency)
/// </summary>
public class ScoreItem
{
    public Guid Id { get; set; }
    public Guid ScoreCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScoreItemType ItemType { get; set; } = ScoreItemType.Rating;
    public decimal MaxScore { get; set; }
    public decimal? WeightWithinCategory { get; set; }
    public bool IsMandatory { get; set; } = false;
    public bool EvidenceRequired { get; set; } = false;
    public string? EvidenceHint { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ScoreCategory Category { get; set; } = null!;
}
