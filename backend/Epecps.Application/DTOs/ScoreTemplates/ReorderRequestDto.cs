using System.ComponentModel.DataAnnotations;

namespace Epecps.Application.DTOs.ScoreTemplates;

/// <summary>
/// DTO for reordering categories or items
/// </summary>
public class ReorderRequestDto
{
    [Required]
    public List<ReorderItemDto> Items { get; set; } = new();
}

public class ReorderItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
