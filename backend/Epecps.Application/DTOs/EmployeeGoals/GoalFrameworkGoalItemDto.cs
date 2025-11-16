namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for goal item (ScoreItem) in the goal framework (read-only for employees)
/// </summary>
public class GoalFrameworkGoalItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetScore { get; set; }
    public List<SuggestedActivityDto> SuggestedActivities { get; set; } = new();
}
