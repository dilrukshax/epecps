using Epecps.Domain.Enums;

namespace Epecps.Domain.Entities;

/// <summary>
/// Represents an individual scoring item within a category (e.g., specific skill or competency)
/// In the context of employee goals, this is equivalent to a "GoalItem"
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
    
    /// <summary>
    /// Default target score for employee goals based on this item (typically 100)
    /// </summary>
    public decimal TargetScore { get; set; } = 100;

    // Navigation properties
    public ScoreCategory Category { get; set; } = null!;
    public ICollection<PersonalGoal> PersonalGoals { get; set; } = new List<PersonalGoal>();
}
