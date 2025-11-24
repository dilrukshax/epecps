namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for score template in the goal framework (read-only for employees)
/// </summary>
public class GoalFrameworkTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public int CategoryCount { get; set; }
}
