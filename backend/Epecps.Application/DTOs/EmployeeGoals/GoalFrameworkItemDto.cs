namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for item in the goal framework (read-only for employees)
/// </summary>
public class GoalFrameworkItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int GoalItemCount { get; set; }
}
