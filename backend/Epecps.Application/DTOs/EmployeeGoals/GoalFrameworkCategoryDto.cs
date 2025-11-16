namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for category in the goal framework (read-only for employees)
/// </summary>
public class GoalFrameworkCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ItemCount { get; set; }
}
