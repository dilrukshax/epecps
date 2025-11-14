using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for score category with its items
/// </summary>
public class ScoreCategoryDto
{
    public Guid Id { get; set; }
    public Guid ScoreTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal? MaxScore { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ScoreItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for score item
/// </summary>
public class ScoreItemDto
{
    public Guid Id { get; set; }
    public Guid ScoreCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScoreItemType ItemType { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? WeightWithinCategory { get; set; }
    public bool IsMandatory { get; set; }
    public bool EvidenceRequired { get; set; }
    public string? EvidenceHint { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
