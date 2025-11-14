namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for listing score templates (summary view)
/// </summary>
public class ScoreTemplateListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public int CategoryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
