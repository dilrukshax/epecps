namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for creating a new personal goal
/// </summary>
public class CreatePersonalGoalDto
{
    public Guid GoalItemId { get; set; }
    
    /// <summary>
    /// Optional: Groups multiple goals created together
    /// If provided, all goals with the same GoalSetId will be displayed as one set
    /// </summary>
    public Guid? GoalSetId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public List<Guid> SelectedSuggestedActivityIds { get; set; } = new();
    public List<string> CustomActivities { get; set; } = new();
}
