namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for creating a new activity for a personal goal
/// </summary>
public class CreatePersonalGoalActivityDto
{
    public Guid? SuggestedActivityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}
