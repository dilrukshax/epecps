namespace Epecps.Domain.Entities;

/// <summary>
/// Represents a category within a scoring template (e.g., Technical Skills, Soft Skills)
/// </summary>
public class ScoreCategory
{
    public Guid Id { get; set; }
    public Guid ScoreTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal? MaxScore { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ScoreTemplate Template { get; set; } = null!;
    public ICollection<ScoreItem> Items { get; set; } = new List<ScoreItem>();
}
