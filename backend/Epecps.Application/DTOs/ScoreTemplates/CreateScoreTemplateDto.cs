using System.ComponentModel.DataAnnotations;

namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for creating a new score template
/// </summary>
public class CreateScoreTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
