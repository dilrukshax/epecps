namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for detailed score template view including categories and items
/// </summary>
public class ScoreTemplateDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public List<ScoreCategoryDto> Categories { get; set; } = new();
}
