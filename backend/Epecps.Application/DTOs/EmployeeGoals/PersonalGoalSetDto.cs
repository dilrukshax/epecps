using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for a set of personal goals created together (grouped view)
/// </summary>
public class PersonalGoalSetDto
{
    /// <summary>
    /// The ID that groups these goals together
    /// </summary>
    public Guid GoalSetId { get; set; }
    
    /// <summary>
    /// Template name used for this goal set
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of goals in this set
    /// </summary>
    public int GoalCount { get; set; }
    
    /// <summary>
    /// Total target score across all goals in the set
    /// </summary>
    public decimal TotalTargetScore { get; set; }
    
    /// <summary>
    /// Total current score across all goals in the set
    /// </summary>
    public decimal TotalCurrentScore { get; set; }
    
    /// <summary>
    /// Start date (same for all goals in the set)
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Due date (same for all goals in the set)
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Overall status of the goal set (based on individual goal statuses)
    /// </summary>
    public PersonalGoalStatus Status { get; set; }
    
    /// <summary>
    /// When the goal set was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Individual goals in this set (for expanded view)
    /// </summary>
    public List<PersonalGoalListDto> Goals { get; set; } = new();
    
    /// <summary>
    /// Categories covered by this goal set
    /// </summary>
    public List<string> Categories { get; set; } = new();
}
