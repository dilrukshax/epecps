namespace Epecps.Domain.Entities;

/// <summary>
/// Represents a suggested activity for a score item (goal item)
/// These are templates that employees can choose from when creating personal goals
/// </summary>
public class SuggestedActivity
{
    public Guid Id { get; set; }
    public Guid ScoreItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ScoreItem ScoreItem { get; set; } = null!;
}
