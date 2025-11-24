using Epecps.Domain.Enums;

namespace Epecps.Domain.Entities;

/// <summary>
/// Represents an employee's personal goal based on a framework GoalItem (ScoreItem)
/// </summary>
public class PersonalGoal
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The user (employee) who owns this goal
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Reference to the framework goal item (ScoreItem)
    /// </summary>
    public Guid GoalItemId { get; set; }
    
    /// <summary>
    /// Groups goals created together in one session (same template, period, dates)
    /// Allows displaying them as a single "goal set" in the UI
    /// </summary>
    public Guid? GoalSetId { get; set; }
    
    /// <summary>
    /// Employee-defined title for this goal
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Employee-defined description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Target score (defaults from GoalItem.TargetScore, typically 100)
    /// </summary>
    public decimal TargetScore { get; set; } = 100;
    
    /// <summary>
    /// Start date of the goal
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Due date for completing the goal
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Current status of the goal
    /// </summary>
    public PersonalGoalStatus Status { get; set; } = PersonalGoalStatus.Draft;
    
    /// <summary>
    /// Current progress/score (0 to TargetScore)
    /// </summary>
    public decimal CurrentScore { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ScoreItem GoalItem { get; set; } = null!;
    public ICollection<PersonalGoalActivity> Activities { get; set; } = new List<PersonalGoalActivity>();
}
