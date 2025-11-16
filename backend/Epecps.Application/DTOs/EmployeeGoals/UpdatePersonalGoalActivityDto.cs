using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for updating an existing personal goal activity
/// </summary>
public class UpdatePersonalGoalActivityDto
{
    public string Description { get; set; } = string.Empty;
    public ActivityStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? EvidenceNotes { get; set; }
}
