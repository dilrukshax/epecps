using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for personal goal activity
/// </summary>
public class PersonalGoalActivityDto
{
    public Guid Id { get; set; }
    public Guid PersonalGoalId { get; set; }
    public Guid? SuggestedActivityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsFromTemplate { get; set; }
    public ActivityStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? EvidenceNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
