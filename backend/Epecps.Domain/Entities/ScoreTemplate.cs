namespace Epecps.Domain.Entities;

/// <summary>
/// Represents a scoring template that defines the structure of an evaluation
/// </summary>
public class ScoreTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsPublished { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }

    // Navigation properties
    public ICollection<ScoreCategory> Categories { get; set; } = new List<ScoreCategory>();
}
